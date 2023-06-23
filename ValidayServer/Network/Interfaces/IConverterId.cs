namespace ValidayServer.Network.Interfaces
{
    public interface IConverterId<TId>
    {
        TId Convert(byte[] bytes);
    }
}