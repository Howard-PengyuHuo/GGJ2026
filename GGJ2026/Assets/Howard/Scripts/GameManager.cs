using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    [Header("Dialogue")]
    public DialogueSystem dialogueSystem;
    public DialogueGraph intro;
    public DialogueGraph level1;
    public DialogueGraph outro;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Intro();
    }

    
    // Update is called once per frame
    void Update()
    {
        
    }

    void Intro()
    {
        dialogueSystem.Play(intro, () =>
        {
            StartLevel();
            //GraphManager.Instance.BuildLevelWLevelData()
        });
    }
    
    void StartLevel()
    {
        dialogueSystem.PlayNPC(level1);
    }



    void GameOver()
    {
        dialogueSystem.Play(outro, () =>
        {
            Intro();
        });
    }
}
