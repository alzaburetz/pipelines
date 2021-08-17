using System.Threading;
using System.Threading.Tasks;

namespace PipelinesServer.Abstractions
{
    /// <summary>
    /// Представляет контракт для создания обертки над Tcp - сервером.
    /// </summary>
    public interface ITcpServer
    {
        /// <summary>
        /// Старт сервера.
        /// </summary>
        void Start(CancellationToken token);

        /// <summary>
        /// Остановка сервера.
        /// </summary>
        void Stop();

        /// <summary>
        /// Ожидание клиента.
        /// </summary>
        void Listen(CancellationToken token);
    }
}
