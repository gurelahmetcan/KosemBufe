namespace Bufe
{
    /// <summary>
    /// Every one-shot sound effect. Add a value here, then map it to a clip in the
    /// AudioManager's SFX Library (Inspector). Call with AudioManager.Instance.PlaySfx(Sfx.X).
    /// Keep <see cref="None"/> first so the default enum value is a safe no-op.
    /// </summary>
    public enum Sfx
    {
        None = 0,
        CashRegister,   // cash.wav
        Click,          // click.wav
        Door,           // door.wav
        Knife,          // knife.wav
        Shutter,        // shutter.wav
        ItemFall,       // stomp.wav
        Toast,          // toast.wav
        Trash,          // trash.wav
        Beep,           // fridge-left-open beep (looped)
    }

    /// <summary>
    /// Every looping music track. Map to a clip in the AudioManager's Music Library.
    /// Call with AudioManager.Instance.PlayMusic(MusicTrack.X).
    /// </summary>
    public enum MusicTrack
    {
        None = 0,
        Main,
    }
}
