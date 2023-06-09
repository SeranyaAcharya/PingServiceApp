namespace Communication
{
    public interface ICommunicationListener
    {
        Task<string> OpenAsync(CancellationToken cancellationToken);

        Task CloseAsync(CancellationToken cancellationToken);

        void Abort();
    }
}