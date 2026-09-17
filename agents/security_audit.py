"""
security_audit.py — Panday Repository Guardian Automated Audit Tool.

Executes Panday's 6-item repository security & Unity version control checklist:
  1. Environment & Secrets (git history + tracked files)
  2. Gitignore & Gitattributes
  3. Git LFS & Asset Serialization Mode
  4. Unity .meta file integrity
  5. History hygiene (Library/, Temp/, Build/ in commits)
  6. Dependency version pinning in requirements.txt

Outputs findings strictly categorized into:
  1. Blocking
  2. Hygiene gaps
  3. Process recommendations
"""

import io
import os
import re
import subprocess
import sys
from pathlib import Path

# Force UTF-8 output on Windows (fixes cp1252 UnicodeEncodeError)
if sys.platform == "win32":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")

# Workspace root is parent of agents/
WORKSPACE_ROOT = Path(__file__).resolve().parent.parent
AGENTS_DIR = Path(__file__).resolve().parent


def run_git(args: list[str]) -> tuple[int, str]:
    """Run a git command in the workspace directory with unquoted paths."""
    try:
        result = subprocess.run(
            ["git", "-c", "core.quotepath=false"] + args,
            cwd=WORKSPACE_ROOT,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )
        return result.returncode, result.stdout.strip()
    except Exception as e:
        return -1, str(e)


def audit_env_and_secrets() -> tuple[list[str], list[str]]:
    """Check for leaked .env, keys, or credentials in git history & working tree."""
    blocking = []
    hygiene = []

    # 1. Check if .env or agents/.env was ever committed in git history
    code, out = run_git(["log", "--all", "--name-only", "--oneline", "--", ".env", "agents/.env", "**/.env"])
    if out:
        blocking.append(
            f"CRITICAL: .env file found in Git history:\n{out}\n"
            "Action required: Treat GEMINI_API_KEY as compromised and rotate it immediately at https://aistudio.google.com/app/api-keys"
        )

    # 2. Check for keystores or secret files committed
    code, tracked_secrets = run_git(["ls-files", "*.keystore", "*.jks", "*.key", "*.pem", "**/google-services.json"])
    if tracked_secrets:
        blocking.append(
            f"CRITICAL: Signing keys or credential files tracked in Git:\n{tracked_secrets}\n"
            "Action required: Untrack and revoke credentials."
        )

    # 3. Check for raw GEMINI_API_KEY assignments in tracked files (excluding .example files)
    code, grep_out = run_git(["grep", "-n", "-i", "GEMINI_API_KEY=AIza"])
    if grep_out:
        blocking.append(
            f"CRITICAL: Active API key string found in tracked files:\n{grep_out}\n"
            "Action required: Rotate API key immediately."
        )

    return blocking, hygiene


def audit_gitignore_gitattributes() -> tuple[list[str], list[str]]:
    """Audit .gitignore and .gitattributes configuration."""
    blocking = []
    hygiene = []

    gitignore_path = WORKSPACE_ROOT / ".gitignore"
    if not gitignore_path.exists():
        blocking.append("Missing .gitignore in repository root.")
    else:
        content = gitignore_path.read_text(encoding="utf-8", errors="replace")
        required_patterns = [
            ("[Ll]ibrary/", "Unity Library folder"),
            ("[Tt]emp/", "Unity Temp folder"),
            (".env", ".env files"),
            ("agents/__pycache__/", "Python __pycache__"),
        ]
        missing = [desc for pattern, desc in required_patterns if pattern not in content]
        if missing:
            hygiene.append(f".gitignore is missing recommended ignore rules for: {', '.join(missing)}")

    gitattributes_path = WORKSPACE_ROOT / ".gitattributes"
    if not gitattributes_path.exists():
        hygiene.append("Missing .gitattributes in repository root (needed for Unity YAML merge drivers & Git LFS).")
    else:
        content = gitattributes_path.read_text(encoding="utf-8", errors="replace")
        if "unityyamlmerge" not in content:
            hygiene.append(".gitattributes does not configure unityyamlmerge drivers for *.unity and *.prefab.")
        if "filter=lfs" not in content:
            hygiene.append(".gitattributes does not configure Git LFS filters for binary formats (*.png, *.wav, *.mp3).")

    return blocking, hygiene


def audit_lfs_and_serialization() -> tuple[list[str], list[str]]:
    """Check Unity serialization mode and large files."""
    blocking = []
    hygiene = []

    # Check EditorSettings.asset for Force Text (m_SerializationMode: 2)
    editor_settings = WORKSPACE_ROOT / "ProjectSettings" / "EditorSettings.asset"
    if editor_settings.exists():
        content = editor_settings.read_text(encoding="utf-8", errors="replace")
        if "m_SerializationMode: 2" not in content:
            blocking.append(
                "Unity Asset Serialization is NOT set to 'Force Text' (m_SerializationMode: 2). "
                "Binary scenes/prefabs will corrupt on merge conflicts."
            )
    else:
        hygiene.append("ProjectSettings/EditorSettings.asset not found — unable to verify serialization mode.")

    return blocking, hygiene


def audit_meta_files() -> tuple[list[str], list[str]]:
    """Audit Unity .meta file integrity across tracked files in Assets/."""
    blocking = []
    hygiene = []

    code, tracked_files = run_git(["ls-files", "Assets/"])
    if code != 0 or not tracked_files:
        return blocking, hygiene

    tracked_set = set(tracked_files.splitlines())
    assets = [f for f in tracked_set if not f.endswith(".meta")]
    meta_files = [f for f in tracked_set if f.endswith(".meta")]

    # Check missing .meta
    missing_meta = []
    for asset in assets:
        expected_meta = asset + ".meta"
        if expected_meta not in tracked_set:
            missing_meta.append(asset)

    if missing_meta:
        blocking.append(
            f"Missing tracked .meta files for {len(missing_meta)} tracked asset(s):\n"
            + "\n".join(f"  • {m}" for m in missing_meta[:10])
            + (f"\n  ... and {len(missing_meta)-10} more" if len(missing_meta) > 10 else "")
        )

    # Check orphaned .meta
    orphaned_meta = []
    for meta in meta_files:
        target = meta[:-5]
        # Target could be file or directory
        target_path = WORKSPACE_ROOT / target
        if not target_path.exists():
            orphaned_meta.append(meta)

    if orphaned_meta:
        hygiene.append(
            f"Found {len(orphaned_meta)} orphaned .meta file(s) whose target asset was deleted:\n"
            + "\n".join(f"  • {m}" for m in orphaned_meta[:10])
        )

    return blocking, hygiene


def audit_history_hygiene() -> tuple[list[str], list[str]]:
    """Check if Library/ or Temp/ were ever committed."""
    blocking = []
    hygiene = []

    code, out = run_git(["log", "--all", "--oneline", "-n", "1", "--", "Library/", "Temp/", "Build/", "Builds/"])
    if out:
        hygiene.append(
            f"Unity temporary build folders found in git history:\n{out}\n"
            "Recommendation: Consider running git filter-repo in a coordinated maintenance window."
        )

    return blocking, hygiene


def audit_dependency_pinning() -> tuple[list[str], list[str]]:
    """Check requirements.txt version pins."""
    blocking = []
    hygiene = []

    req_path = AGENTS_DIR / "requirements.txt"
    if not req_path.exists():
        hygiene.append("agents/requirements.txt not found.")
    else:
        lines = req_path.read_text(encoding="utf-8", errors="replace").splitlines()
        for line in lines:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            if "==" not in line and ">=" not in line and "<=" not in line and "~=" not in line:
                hygiene.append(f"Unpinned dependency in requirements.txt: '{line}' (reproducibility risk).")

    return blocking, hygiene


def run_audit() -> int:
    """Run all Panday checks and print formatted report."""
    print("=" * 65)
    print("  🛡️  PANDAY — Repository Security & Unity Hygiene Audit")
    print("=" * 65 + "\n")

    all_blocking: list[str] = []
    all_hygiene: list[str] = []
    recommendations: list[str] = []

    checks = [
        ("Environment & Secrets", audit_env_and_secrets),
        ("Gitignore & Gitattributes", audit_gitignore_gitattributes),
        ("LFS & Asset Serialization", audit_lfs_and_serialization),
        ("Unity .meta File Integrity", audit_meta_files),
        ("Git History Hygiene", audit_history_hygiene),
        ("Dependency Pinning", audit_dependency_pinning),
    ]

    for name, check_func in checks:
        b, h = check_func()
        all_blocking.extend(b)
        all_hygiene.extend(h)

    # Standard process recommendations
    recommendations.append("Ensure pre-commit hooks or CI runs 'python agents/security_audit.py' before merges.")
    recommendations.append("Set up Git LFS on all collaborator machines: 'git lfs install'.")
    recommendations.append("Never run 'git filter-repo' without backing up the repository and obtaining team confirmation.")

    # 1. Blocking
    print("1. ⛔ BLOCKING FINDINGS")
    if all_blocking:
        for item in all_blocking:
            print(f"\n[!] {item}")
    else:
        print("   ✅ None. No live secrets, corrupted serialization, or missing .meta files found.")

    # 2. Hygiene Gaps
    print("\n2. ⚠️  HYGIENE GAPS")
    if all_hygiene:
        for item in all_hygiene:
            print(f"\n[*] {item}")
    else:
        print("   ✅ None. .gitignore, .gitattributes, and dependencies are properly configured.")

    # 3. Process Recommendations
    print("\n3. 💡 PROCESS RECOMMENDATIONS")
    for rec in recommendations:
        print(f"   • {rec}")

    print("\n" + "=" * 65)
    if all_blocking:
        print("  STATUS: AUDIT FAILED (Blocking issues detected)")
        print("=" * 65)
        return 1
    elif all_hygiene:
        print("  STATUS: PASSED WITH WARNINGS (Hygiene gaps detected)")
        print("=" * 65)
        return 0
    else:
        print("  STATUS: CLEAN (All checks passed)")
        print("=" * 65)
        return 0


if __name__ == "__main__":
    sys.exit(run_audit())
