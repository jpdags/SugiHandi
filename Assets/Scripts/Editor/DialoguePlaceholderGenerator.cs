using UnityEngine;
using UnityEditor;
using System.IO;

public class DialoguePlaceholderGenerator : EditorWindow
{
    [MenuItem("SugiHandi Tools/Generate Placeholder Dialogues")]
    public static void GeneratePlaceholders()
    {
        string dataPath = "Assets/Resources/Data";
        string dbPath = dataPath + "/DialogueDatabase.asset";

        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        // Create Database if it doesn't exist
        DialogueDatabase db = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<DialogueDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        // Generate placeholders
        string[] placeholderTriggers = { "Elder_Greeting", "Fisherman_Quest", "Merchant_Trade" };
        
        foreach (string trigger in placeholderTriggers)
        {
            // Check if already in DB
            if (db.dialogueEntries.Exists(e => e.triggerId == trigger))
                continue;

            string assetPath = $"{dataPath}/Dialogue_{trigger}.asset";
            DialogueData dialogueAsset = AssetDatabase.LoadAssetAtPath<DialogueData>(assetPath);

            if (dialogueAsset == null)
            {
                dialogueAsset = ScriptableObject.CreateInstance<DialogueData>();
                dialogueAsset.npcName = trigger.Split('_')[0];
                dialogueAsset.startNodeId = "node_0";

                // Node 0
                DialogueNode startNode = new DialogueNode();
                startNode.nodeId = "node_0";
                startNode.npcText = $"Hello traveler. I am the {dialogueAsset.npcName}. I know of the {{Gabok}}.";
                
                // Option 1
                DialogueOption option1 = new DialogueOption();
                option1.optionText = "Tell me more.";
                option1.npcResponse = "It is a dangerous path. Seek the {balangay} for answers.";
                option1.nextNodeId = "node_1";
                
                // Option 2
                DialogueOption option2 = new DialogueOption();
                option2.optionText = "Goodbye.";
                option2.npcResponse = "Farewell, {manaog}.";
                option2.nextNodeId = ""; // End

                startNode.options.Add(option1);
                startNode.options.Add(option2);
                dialogueAsset.nodes.Add(startNode);

                // Node 1
                DialogueNode node1 = new DialogueNode();
                node1.nodeId = "node_1";
                node1.npcText = "What else do you wish to know?";
                
                DialogueOption option3 = new DialogueOption();
                option3.optionText = "I must go now.";
                option3.npcResponse = "May the spirits guide you.";
                option3.nextNodeId = "";
                
                node1.options.Add(option3);
                dialogueAsset.nodes.Add(node1);

                AssetDatabase.CreateAsset(dialogueAsset, assetPath);
            }

            // Add to database
            DialogueDatabase.DialogueTriggerEntry entry = new DialogueDatabase.DialogueTriggerEntry();
            entry.triggerId = trigger;
            entry.dialogueData = dialogueAsset;
            db.dialogueEntries.Add(entry);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Placeholder dialogues generated at " + dataPath);
    }
}
