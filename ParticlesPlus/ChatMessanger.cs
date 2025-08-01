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
        private readonly ICoreClientAPI _capi;

        private readonly string _modName;
        private readonly string successColor = "#5FED6F";
        private readonly string failColor = "#ED5E60";


        public ChatMessanger(ICoreClientAPI capi, ModSystem modSystem)
        {
            _capi = capi;
            _modSystem = modSystem;
            _modName = _modSystem.Mod.Info.Name;
        }

        public void ShowMessage(string messageBody, MessageType type)
        {
            string messageColor = type switch
            {
                MessageType.Success => successColor,
                MessageType.Error => failColor,
                _ => "#FFFFFF",
            };

            string message = $"[{_modName}]: {messageBody}";

            _capi.ShowChatMessage($"<strong><font color='{messageColor}'>{message}</font></strong>");
        }
    }
}
