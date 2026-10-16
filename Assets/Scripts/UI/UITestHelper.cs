using UnityEngine;

// SADECE TEST İÇİN. AnaCanvas prefab'ına girmeyecek.
public class UITestHelper : MonoBehaviour
{
    public void GunArtir()      { GameManager.Instance.gun++; }
    public void ParaEkle()      { GameManager.Instance.para += 100; }

    public void OkulaTikla()    { BinaPopup.Instance.Ac(BinaTuru.Okul); }
    public void ItfaiyeyeTikla() { BinaPopup.Instance.Ac(BinaTuru.Itfaiye); }
    public void AkutaTikla()    { BinaPopup.Instance.Ac(BinaTuru.Akut); }
}