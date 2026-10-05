using UnityEngine;
using UnityEngine.InputSystem;

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

    public static bool Voltar()
    {
        var teclado = Keyboard.current;
        var controle = Gamepad.current;
        return (teclado != null && teclado.escapeKey.wasPressedThisFrame)
            || (controle != null && controle.selectButton.wasPressedThisFrame);
    }
}
