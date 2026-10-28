using UnityEngine;

namespace TempoParado
{
    public sealed class LabHUD : MonoBehaviour
    {
        public TimeBody player;
        private void OnGUI()
        {
            if (player == null) return;
            GUI.Box(new Rect(12, 12, 390, 190), "TEMPO PARADO | TimeLab - terceira pessoa");
            GUI.Label(new Rect(24, 40, 365, 25), "WASD / setas: mover | I: protecao | R: reiniciar");
            GUI.Label(new Rect(24, 66, 365, 25), "Protecao: " + (player.ignoreZones ? "LIGADA" : "DESLIGADA") + " | Ritmo: " + player.LocalTimeScale.ToString("0.0") + "x");
            GUI.Label(new Rect(24, 92, 365, 25), "Verde 1x | Azul 0.2x | Laranja 2x");
            GUI.Label(new Rect(24, 118, 365, 40), "Fora das cores: mundo congelado.\nSe ficar preso, pressione I para voltar a andar.");
            GUI.Label(new Rect(24, 163, 365, 25), "Mouse: camera | Esc: cursor | Clique: voltar");
        }
    }
}
