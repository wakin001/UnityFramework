using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HotFix_Project
{
    public static class AwaitExtension
    {
        /// <summary>
        /// Add a custom GetAwaiter extension method to the TimeSpan class
        /// </summary>
        public static TaskAwaiter GetAwaiter(this TimeSpan timeSpan)
        {
            return Task.Delay(timeSpan).GetAwaiter();
        }
    }

}
