namespace SignalisVrTracking
{
    public sealed class DiagnosticFlow
    {
        public int Stage { get; private set; }
        private float deadline;
        public bool CanSubmit { get { return Stage == 3; } }
        public void Connected() { if (Stage == 0) Stage = 1; }
        public void RenderReady() { if (Stage == 1) Stage = 2; }
        public bool BeginSubmit(float now)
        {
            if (Stage != 2) return false;
            deadline = now + 300f; Stage = 3; return true;
        }
        public bool Expired(float now) { return Stage == 3 && now >= deadline; }
        public void Stop() { Stage = 0; }
    }
}
