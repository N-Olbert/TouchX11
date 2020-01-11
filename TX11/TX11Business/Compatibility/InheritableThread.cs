using System.Threading;
using JetBrains.Annotations;

namespace TX11Business.Compatibility
{
    internal abstract class InheritableThread
    {
        [NotNull]
        private readonly Thread thread;

        protected InheritableThread()
        {
            thread = new Thread(this.Run);
        }

        internal void Start() => thread.Start();

        internal void Join() => thread.Join();

        internal abstract void Run();
    }
}