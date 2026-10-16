using UnityEngine;

[CreateAssetMenu(fileName = "Soru", menuName = "Quiz/Soru")]
public class QuestionSO : ScriptableObject
{
    [TextArea] public string soruMetni;
    public string[] siklar = new string[4];
    public int dogruSikIndex; // 0'dan başlar: 0 = ilk şık
}