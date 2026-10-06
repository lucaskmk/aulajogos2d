using UnityEngine;

// Partículas de clima flutuando na frente do cenário, sempre em volta da câmera:
//  - Polen:     pontinhos claros subindo devagar, balançando (dia normal);
//  - Petalas:   pétalas cor-de-rosa caindo (fases rosadas);
//  - VagaLumes: luzinhas amarelas que piscam e iluminam em volta (fase noturna);
//  - Poeira:    poeira dourada parada no ar (biblioteca);
//  - Brilhos:   estrelinhas (fase secreta).
[DefaultExecutionOrder(110)] // depois da câmera e da paralaxe
public class Ambiente : MonoBehaviour
{
    public enum Tipo { Polen, Petalas, VagaLumes, Poeira, Brilhos }

    Tipo tipo;
    Transform[] particulas;
    SpriteRenderer[] desenhos;
    Vector2[] velocidades;
    float[] fases;

    public static void Criar(Transform raiz, Tipo tipo, int quantidade)
    {
        var ambiente = new GameObject("Ambiente " + tipo).AddComponent<Ambiente>();
        ambiente.transform.SetParent(raiz, false);
        ambiente.tipo = tipo;
        ambiente.particulas = new Transform[quantidade];
        ambiente.desenhos = new SpriteRenderer[quantidade];
        ambiente.velocidades = new Vector2[quantidade];
        ambiente.fases = new float[quantidade];

        Camera cam = Camera.main;
        float cx = cam != null ? cam.transform.position.x : 0f;
        for (int i = 0; i < quantidade; i++)
        {
            string sprite = tipo == Tipo.Brilhos ? "brilho" : "poeira";
            var p = ConstrutorDeFase.Visual("Particula", ambiente.transform, new Vector3(cx + Random.Range(-15f, 15f), Random.Range(0f, 15f), 0f), sprite, 15, false);
            ambiente.particulas[i] = p.transform;
            ambiente.desenhos[i] = p.GetComponent<SpriteRenderer>();
            ambiente.fases[i] = Random.Range(0f, 10f);
            ambiente.Preparar(i);
            if (tipo == Tipo.VagaLumes && i % 2 == 0)
                Luzes.Ponto(p.transform, new Color(1f, 0.95f, 0.5f), 1.6f, 0.6f);
        }
    }

    void Preparar(int i)
    {
        float tamanho;
        Color cor;
        switch (tipo)
        {
            case Tipo.Petalas:
                velocidades[i] = new Vector2(Random.Range(0.4f, 1.2f), Random.Range(-1.2f, -0.6f));
                tamanho = Random.Range(0.35f, 0.55f);
                cor = new Color(1f, 0.7f, 0.8f, 0.9f);
                break;
            case Tipo.VagaLumes:
                velocidades[i] = Random.insideUnitCircle * 0.4f;
                tamanho = 0.3f;
                cor = new Color(1f, 0.95f, 0.5f);
                break;
            case Tipo.Poeira:
                velocidades[i] = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.05f, 0.1f));
                tamanho = Random.Range(0.15f, 0.3f);
                cor = new Color(1f, 0.9f, 0.6f, 0.7f);
                break;
            case Tipo.Brilhos:
                velocidades[i] = new Vector2(0f, Random.Range(0.2f, 0.5f));
                tamanho = Random.Range(0.3f, 0.6f);
                cor = new Color(1f, 1f, 0.8f, 0.9f);
                break;
            default: // Polen
                velocidades[i] = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.15f, 0.4f));
                tamanho = Random.Range(0.15f, 0.3f);
                cor = new Color(1f, 1f, 0.85f, 0.65f);
                break;
        }
        particulas[i].localScale = Vector3.one * tamanho;
        desenhos[i].color = cor;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        float meia = cam.orthographicSize * cam.aspect + 1f;
        float cx = cam.transform.position.x, dt = Time.deltaTime, t = Time.time;

        for (int i = 0; i < particulas.Length; i++)
        {
            Vector3 p = particulas[i].position;
            float balanco = Mathf.Sin(t * 1.5f + fases[i]);
            p.x += (velocidades[i].x + balanco * 0.3f) * dt;
            p.y += velocidades[i].y * dt;

            // saiu da tela? volta pelo outro lado
            if (p.x < cx - meia) p.x += meia * 2f;
            if (p.x > cx + meia) p.x -= meia * 2f;
            if (p.y < -0.5f) p.y += 15.5f;
            if (p.y > 15f) p.y -= 15.5f;
            particulas[i].position = p;

            if (tipo == Tipo.Petalas) particulas[i].rotation = Quaternion.Euler(0f, 0f, balanco * 60f);
            if (tipo == Tipo.VagaLumes || tipo == Tipo.Brilhos)
            {
                Color c = desenhos[i].color;
                c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 2f + fases[i]));
                desenhos[i].color = c;
            }
        }
    }
}
