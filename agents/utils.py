"""
utils.py — Shared utilities for SugiHandi Game Design Agent Pipeline.

Provides:
  - Robust, string-aware JSON extraction from model outputs.
  - Raw response persistence for debugging and traceability.
  - Standardized agent retry and execution wrappers.
"""

import asyncio
import json
import logging
import re
from datetime import datetime
from pathlib import Path
from typing import Any, Callable, Coroutine, Type, TypeVar

from google.antigravity.types import (
    ModelAPIRetryConfig,
    ModelOutputRetryConfig,
    RetryConfig,
)
from pydantic import BaseModel, ValidationError

logger = logging.getLogger("sugi_agents")

T = TypeVar("T", bound=BaseModel)

AGENTS_DIR = Path(__file__).resolve().parent
RAW_OUTPUT_DIR = AGENTS_DIR / "output" / "_raw"


def get_default_retry_config() -> RetryConfig:
    """Return default retry configuration for Google Antigravity agents."""
    return RetryConfig(
        api_retry=ModelAPIRetryConfig(
            max_retries=3,
            initial_sleep_duration_ms=1000,
            exponential_multiplier=2.0,
            jitter_range=0.2,
        ),
        model_output_retry=ModelOutputRetryConfig(
            max_retries=2,
        ),
    )


def save_raw_response(agent_name: str, raw_text: str) -> Path:
    """
    Save the raw, unparsed model response to output/_raw/<agent_name>.txt.
    Always called BEFORE parsing so generation context is preserved on error.
    """
    RAW_OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    raw_path = RAW_OUTPUT_DIR / f"{agent_name}.txt"
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

    with open(raw_path, "w", encoding="utf-8") as f:
        f.write(f"// Agent: {agent_name} | Timestamp: {timestamp}\n")
        f.write(raw_text)

    return raw_path


def extract_json(text: str) -> dict:
    """
    Extract a valid JSON dictionary from model output.

    Handles:
      - Markdown code blocks (```json ... ``` or ``` ... ```)
      - Strings containing braces (e.g. Butuanon inscriptions or punctuation)
      - Escaped characters inside string literals
      - Leading/trailing conversational commentary
    """
    if not text or not isinstance(text, str):
        raise ValueError("Cannot extract JSON from empty or non-string input.")

    stripped = text.strip()

    # 1. Strip markdown code fence blocks if present
    stripped = re.sub(r"^```(?:json)?\s*", "", stripped, flags=re.MULTILINE)
    stripped = re.sub(r"\s*```$", "", stripped, flags=re.MULTILINE)
    stripped = stripped.strip()

    # 2. Try direct parsing first
    try:
        data = json.loads(stripped)
        if isinstance(data, dict):
            return data
    except json.JSONDecodeError:
        pass

    # 3. String-aware search for balanced curly braces
    start = stripped.find("{")
    if start == -1:
        raise ValueError(f"No JSON object found in response: {text[:150]}...")

    depth = 0
    in_string = False
    escape = False

    for i in range(start, len(stripped)):
        char = stripped[i]

        if in_string:
            if escape:
                escape = False
            elif char == "\\":
                escape = True
            elif char == '"':
                in_string = False
        else:
            if char == '"':
                in_string = True
            elif char == "{":
                depth += 1
            elif char == "}":
                depth -= 1
                if depth == 0:
                    candidate = stripped[start : i + 1]
                    try:
                        parsed = json.loads(candidate)
                        if isinstance(parsed, dict):
                            return parsed
                    except json.JSONDecodeError:
                        continue

    # 4. Fallback: regex search for outer braces
    matches = re.findall(r"(\{.*\})", stripped, re.DOTALL)
    for match in reversed(matches):
        try:
            parsed = json.loads(match)
            if isinstance(parsed, dict):
                return parsed
        except json.JSONDecodeError:
            continue

    raise ValueError(f"Could not parse valid JSON object from model response:\n{text[:250]}...")


async def run_with_timeout_and_retry(
    agent_name: str,
    action: Callable[[], Coroutine[Any, Any, Any]],
    schema_cls: Type[T] | None = None,
    timeout_seconds: float = 120.0,
    max_attempts: int = 2,
    backoff_factor: float = 2.0,
) -> dict:
    """
    Execute an agent action with timeout, retry backoff, and Pydantic validation.
    """
    last_error: Exception | None = None

    for attempt in range(1, max_attempts + 1):
        try:
            # Enforce timeout to prevent hanging calls
            result = await asyncio.wait_for(action(), timeout=timeout_seconds)

            # If result is already a validated model or dict
            if isinstance(result, BaseModel):
                data = result.model_dump()
            elif isinstance(result, dict):
                data = result
            elif isinstance(result, str):
                save_raw_response(agent_name, result)
                data = extract_json(result)
            else:
                data = dict(result)

            # Validate against Pydantic schema if provided
            if schema_cls is not None:
                validated_model = schema_cls.model_validate(data)
                return validated_model.model_dump()

            return data

        except asyncio.TimeoutError as te:
            last_error = TimeoutError(
                f"[{agent_name}] Attempt {attempt}/{max_attempts} timed out after {timeout_seconds}s"
            )
            print(f"⚠️  {last_error}")
        except ValidationError as ve:
            last_error = ValueError(
                f"[{agent_name}] Schema validation failed on attempt {attempt}/{max_attempts}:\n{ve}"
            )
            print(f"⚠️  {last_error}")
        except Exception as e:
            last_error = e
            print(f"⚠️  [{agent_name}] Attempt {attempt}/{max_attempts} failed: {e}")

        if attempt < max_attempts:
            sleep_time = backoff_factor ** (attempt - 1)
            print(f"   ⏳ Retrying in {sleep_time:.1f}s...")
            await asyncio.sleep(sleep_time)

    raise last_error or RuntimeError(f"[{agent_name}] Failed after {max_attempts} attempts.")
