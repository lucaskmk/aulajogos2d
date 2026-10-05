using UnityEngine;

// Qualquer coisa que mata o jogador ao encostar (espinho, serra, esmagador...).
// Precisa de um Collider2D marcado como "Is Trigger" no mesmo objeto.
public class Perigo : MonoBehaviour
{
    public bool ativo = true;

    void OnTriggerEnter2D(Collider2D outro) => Encostou(outro);
    void OnTriggerStay2D(Collider2D outro) => Encostou(outro);

    void Encostou(Collider2D outro)
    {
        if (!ativo) return;
        var jogador = outro.GetComponent<Jogador>();
        if (jogador != null) jogador.Morrer();
    }

    // Ajudante para criar a "área que mata" de um objeto.
    public static Perigo Adicionar(GameObject objeto, Vector2 tamanho, Vector2 deslocamento)
    {
        var area = objeto.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = tamanho;
        area.offset = deslocamento;
        return objeto.AddComponent<Perigo>();
    }

    // Objetos que se movem precisam de um Rigidbody2D cinemático para a física funcionar direito.
    public static void TornarMovel(GameObject objeto)
    {
        var corpo = objeto.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Kinematic;
    }
}
