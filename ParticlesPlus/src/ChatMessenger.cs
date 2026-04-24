using Vintagestory.API.Client;

namespace ParticlesPlus
{
    public enum MessegeType
    {
        Error,
        Success
    }
    public class ChatMessenger(ModSystem modSystem)
    {
        private readonly ModSystem _modSystem = modSystem;
        private ICoreClientAPI API => _modSystem.API;
        private string ModName => _modSystem.Mod.Info.Name;

        private readonly string successColor = "#5CAE63";
        private readonly string errorColor = "#D75F4C";

        public void ShowMessege(string messageBody, MessegeType type)
        {
            string messageColor = type switch
            {
                MessegeType.Success => successColor,
                MessegeType.Error => errorColor,
                _ => "#FFFFFF",
            };

            string message = $"[{ModName}]: {messageBody}";

            API.ShowChatMessage($"<strong><font color='{messageColor}'>{message}</font></strong>");
        }
    }
}
