namespace Simple.Data
{
    public class SimpleOrderByItem
    {
        private readonly OrderByDirection _direction;
        private readonly ObjectReference _reference;

        public SimpleOrderByItem(ObjectReference reference) : this(reference, OrderByDirection.Ascending)
        {
        }

        public SimpleOrderByItem(ObjectReference reference, OrderByDirection direction)
        {
            _reference = reference;
            _direction = direction;
        }

        public OrderByDirection Direction
        {
            get { return _direction; }
        }

        public ObjectReference Reference
        {
            get { return _reference; }
        }
    }
}