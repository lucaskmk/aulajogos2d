using UnityEngine;

// 's' - ponto de save do Retorno pela Morte. Encostou: daqui pra frente você renasce aqui.
// Mas às vezes (como no anime) o ponto de save "muda de lugar" e você volta para o começo. :)
// (quem decide isso é o GerenciadorDoJogo, quando você morre)
public class PontoDeSave : MonoBehaviour
{
    SpriteRenderer desenho;
    Transform cristal;
    bool ativo;

    void Awake()
    {
        var filho = ConstrutorDeFase.Visual("Cristal", transform, transform.position, "ponto_save", -1);
        cristal = filho.transform;
        desenho = filho.GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        ativo = GerenciadorDoJogo.Instancia.EhPontoDeSave(LugarDeRenascer);
        desenho.sprite = FabricaDeSprites.Pegar(ativo ? "ponto_save_ativo" : "ponto_save");
    }

    // A posição do objeto é o chão; o jogador renasce no meio do bloco, como no 'P'.
    Vector3 LugarDeRenascer => transform.position + Vector3.up * 0.5f;

    void Update()
    {
        // ativo, o cristal flutua e "respira"
        if (ativo) cristal.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.time * 4f));

        if (ativo || !GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return;
        Vector2 d = jogador - (Vector2)transform.position;
        if (Mathf.Abs(d.x) < 0.7f && d.y > -0.2f && d.y < 2f) Ativar();
    }

    void Ativar()
    {
        ativo = true;
        desenho.sprite = FabricaDeSprites.Pegar("ponto_save_ativo");
        GerenciadorDoJogo.Instancia.SalvarPonto(LugarDeRenascer);
        GerenciadorDoJogo.Som("moeda");
        GerenciadorDoJogo.Instancia.Avisar("Ponto de save! (confia?)", 2f);
        Efeitos.Poeira(transform.parent, transform.position + Vector3.up, 8, 2.5f);
    }
}
