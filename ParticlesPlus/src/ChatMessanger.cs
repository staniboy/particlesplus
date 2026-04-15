using Vintagestory.API.Client;

namespace ParticlesPlus
{
    public enum MessageType
    {
        Error,
        Success
    }
    public class ChatMessanger(ModSystem modSystem)
    {
        private readonly ModSystem _modSystem = modSystem;
        private ICoreClientAPI API => _modSystem.API;
        private string ModName => _modSystem.Mod.Info.Name;

        private readonly string successColor = "#5CAE63";
        private readonly string errorColor = "#D75F4C";

        public void ShowMessage(string messageBody, MessageType type)
        {
            string messageColor = type switch
            {
                MessageType.Success => successColor,
                MessageType.Error => errorColor,
                _ => "#FFFFFF",
            };

            string message = $"[{ModName}]: {messageBody}";

            API.ShowChatMessage($"<strong><font color='{messageColor}'>{message}</font></strong>");
        }
    }
}
