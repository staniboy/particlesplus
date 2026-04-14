using Vintagestory.API.Client;

namespace ParticlesPlus
{
    enum MessageType
    {
        Error,
        Success
    }
    internal class ChatMessanger
    {
        private readonly ModSystem _modSystem;
        private readonly ICoreClientAPI _api;
        private readonly string _modName;

        private readonly string successColor = "#5CAE63";
        private readonly string errorColor = "#D75F4C";


        public ChatMessanger(ModSystem modSystem)
        {
            _modSystem = modSystem;
            _api = _modSystem.capi;
            _modName = _modSystem.Mod.Info.Name;
        }

        public void ShowMessage(string messageBody, MessageType type)
        {
            string messageColor = type switch
            {
                MessageType.Success => successColor,
                MessageType.Error => errorColor,
                _ => "#FFFFFF",
            };

            string message = $"[{_modName}]: {messageBody}";

            _api.ShowChatMessage($"<strong><font color='{messageColor}'>{message}</font></strong>");
        }
    }
}
