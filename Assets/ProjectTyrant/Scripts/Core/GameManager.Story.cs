namespace ProjectTyrant
{
    /// <summary>Lauf 6: Zugriff auf Geschichte und Szenen-Abspieler (Komponenten am GameManager-Objekt, keine eigenen Singletons).</summary>
    public partial class GameManager
    {
        StoryDirector _story; DialoguePlayer _dialogue;
        public StoryDirector Story => _story != null ? _story : (_story = GetComponent<StoryDirector>());
        public DialoguePlayer Dialogue => _dialogue != null ? _dialogue : (_dialogue = GetComponent<DialoguePlayer>());
    }
}
