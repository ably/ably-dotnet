namespace IO.Ably.Diff.Vcdiff
{
    /// <summary>
    /// Contains the information for a single instruction.
    /// </summary>
    internal struct Instruction
    {
        private readonly InstructionType _type;

        internal InstructionType Type => _type;

        private readonly byte _size;

        internal byte Size => _size;

        private readonly byte _mode;

        internal byte Mode => _mode;

        internal Instruction(InstructionType type, byte size, byte mode)
        {
            _type = type;
            _size = size;
            _mode = mode;
        }
    }
}
