public class DoSetSequence : DoAction
{
    AvatarSequencer sequencer;
    SequenceState oldState;
    SequenceState newState;

    public DoSetSequence(DoType type, AvatarSequencer _sequencer, SequenceState _oldState, SequenceState _newState) : base(type)
    {
        sequencer = _sequencer;
        oldState = _oldState;
        newState = _newState;
    }

    public override bool Undo()
    {
        if (sequencer == null) return false;

        sequencer.Play(oldState);

        return true;
    }

    public override bool Redo()
    {
        if (sequencer == null) return false;

        sequencer.Play(newState);

        return true;
    }
}
