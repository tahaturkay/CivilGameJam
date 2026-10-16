using UnityEngine;

public class UITestHelper : MonoBehaviour
{
    public void GunArtir()  { GameManager.Instance.gun++; }
    public void ParaEkle()  { GameManager.Instance.para += 100; }
    public void OkulaTikla() { QuizManager.Instance.QuizAc(); } // 2. kişinin tıklamasını taklit eder
}