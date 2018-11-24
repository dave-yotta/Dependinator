namespace Dependinator.Test
{    
    public class TestNode
    {
        public int Id { get; private set; }
        public static TestNode Create( int id)
        {
            return new TestNode { Id = id };
        }

        public override bool Equals(object obj)
        {
            var node = obj as TestNode;
            return node != null &&
                   Id == node.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + Id.GetHashCode();
        }
    }
}
