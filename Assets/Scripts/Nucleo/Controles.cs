using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Leitura de teclado e controle (usa o Input System novo do Unity).
// Fica tudo aqui para que o resto do jogo não precise saber qual tecla foi apertada.
public static class Controles
{
    public static float Horizontal()
    {
        float x = 0f;

        var teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado.leftArrowKey.isPressed || teclado.aKey.isPressed) x -= 1f;
            if (teclado.rightArrowKey.isPressed || teclado.dKey.isPressed) x += 1f;
        }

        var controle = Gamepad.current;
        if (controle != null && x == 0f)
        {
            float analogico = controle.leftStick.x.ReadValue();
            if (Mathf.Abs(analogico) > 0.3f) x = Mathf.Sign(analogico);
            if (controle.dpad.left.isPressed) x = -1f;
            if (controle.dpad.right.isPressed) x = 1f;
        }

        return x;
    }

    public static bool PuloApertou()
    {
        var teclado = Keyboard.current;
        var controle = Gamepad.current;
        return (teclado != null && (teclado.spaceKey.wasPressedThisFrame || teclado.upArrowKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame))
            || (controle != null && controle.buttonSouth.wasPressedThisFrame);
    }

    public static bool PuloSegurando()
    {
        var teclado = Keyboard.current;
        var controle = Gamepad.current;
        return (teclado != null && (teclado.spaceKey.isPressed || teclado.upArrowKey.isPressed || teclado.wKey.isPressed))
            || (controle != null && controle.buttonSouth.isPressed);
    }

    public static bool Confirmar()
    {
        var teclado = Keyboard.current;
        var controle = Gamepad.current;
        return (teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame))
            || (controle != null && (controle.startButton.wasPressedThisFrame || controle.buttonSouth.wasPressedThisFrame));
    }

    public static bool Reiniciar()
    {
        var teclado = Keyboard.current;
        var controle = Gamepad.current;
        return (teclado != null && teclado.rKey.wasPressedThisFrame)
            || (controle != null && controle.buttonNorth.wasPressedThisFrame);
    }

    // Esc: pausa e despausa (no controle, Start ou Select).
    public static bool Pausar()
    {
        var teclado = Keyboard.current;
        var controle = Gamepad.current;
        return (teclado != null && teclado.escapeKey.wasPressedThisFrame)
            || (controle != null && (controle.selectButton.wasPressedThisFrame || controle.startButton.wasPressedThisFrame));
    }

    // Q: na pausa, volta para o título (no controle, o botão B / bolinha).
    public static bool SairParaOTitulo()
    {
        var teclado = Keyboard.current;
        var controle = Gamepad.current;
        return (teclado != null && teclado.qKey.wasPressedThisFrame)
            || (controle != null && controle.buttonEast.wasPressedThisFrame);
    }

    // Setas, WASD ou o direcional do controle, para andar nos menus (só no quadro em que apertou).
    public static bool CimaApertou() => Apertou(t => t.upArrowKey, t => t.wKey, c => c.dpad.up);
    public static bool BaixoApertou() => Apertou(t => t.downArrowKey, t => t.sKey, c => c.dpad.down);
    public static bool EsquerdaApertou() => Apertou(t => t.leftArrowKey, t => t.aKey, c => c.dpad.left);
    public static bool DireitaApertou() => Apertou(t => t.rightArrowKey, t => t.dKey, c => c.dpad.right);

    // F9: libera todas as fases do mapa (atalho para testar as fases e o chefe sem jogar tudo de novo).
    public static bool LiberarTudo()
    {
        var teclado = Keyboard.current;
        return teclado != null && teclado.f9Key.wasPressedThisFrame;
    }

    static bool Apertou(System.Func<Keyboard, KeyControl> seta, System.Func<Keyboard, KeyControl> letra, System.Func<Gamepad, ButtonControl> direcional)
    {
        var teclado = Keyboard.current;
        var controle = Gamepad.current;
        return (teclado != null && (seta(teclado).wasPressedThisFrame || letra(teclado).wasPressedThisFrame))
            || (controle != null && direcional(controle).wasPressedThisFrame);
    }
}
