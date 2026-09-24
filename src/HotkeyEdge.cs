namespace SignalisVrTracking
{
    public sealed class HotkeyEdge
    {
        private bool wasKeyDown;
        public bool Poll(bool keyDown)
        {
            bool pressed = keyDown && !wasKeyDown;
            wasKeyDown = keyDown;
            return pressed;
        }
    }
}
