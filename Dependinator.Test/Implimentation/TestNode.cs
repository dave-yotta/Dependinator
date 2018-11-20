namespace Dependinator.Test
{    
    public class TestNode
    {
        public int Id { get; private set; }
        public static TestNode Create( int id)
        {
            return new TestNode { Id = id };
        }
    }
}
