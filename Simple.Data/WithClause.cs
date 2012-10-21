namespace Simple.Data
{
    public class WithClause : SimpleQueryClauseBase
    {
        private readonly WithMode _mode;
        private readonly ObjectReference _objectReference;

        public WithClause(ObjectReference objectReference) : this(objectReference, WithType.NotSpecified)
        {
        }

        public WithClause(ObjectReference objectReference, WithType type)
            : this(objectReference, WithMode.NotSpecified, type)
        {
        }

        public WithClause(ObjectReference objectReference, WithMode mode)
            : this(objectReference, mode, WithType.NotSpecified)
        {
        }

        public WithClause(ObjectReference objectReference, WithMode mode, WithType type)
        {
            _objectReference = objectReference;
            _mode = mode;
            Type = type;
        }

        public WithMode Mode
        {
            get { return _mode; }
        }

        public WithType Type { get; set; }

        public ObjectReference ObjectReference
        {
            get { return _objectReference; }
        }
    }
}