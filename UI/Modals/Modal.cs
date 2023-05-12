using Cornifer.UI.Elements;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Cornifer.UI.Modals
{
    public abstract class Modal<TModal, TResult> : UIModal where TModal : UIModal, new()
    {
        private static bool modalVisible;
        private static TaskCompletionSource<TResult>? taskCompletionSource;

        public static TModal? Instance { get; protected set; }
        public static Modal<TModal, TResult>? ModalInstance => Instance as Modal<TModal, TResult>;

        public static Task<TResult> Task 
        {
            get
            {
                taskCompletionSource ??= new();
                return taskCompletionSource.Task;
            }
        }

        public static bool ModalVisible
        {
            get => modalVisible;
            set
            {
                if (modalVisible == value)
                    return;

                modalVisible = value;

                if (value)
                {
                    Interface.CurrentModal = Instance;
                    ModalInstance?.Shown();
                }

                if (Instance is not null)
                    Instance.Visible = value;

                if (!value)
                {
                    Interface.CurrentModal = null;
                    ModalInstance?.Hidden();
                    Interface.ModalClosed();
                }

                if (!value && taskCompletionSource is not null)
                {
                    if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                        taskCompletionSource.SetCanceled();
                    taskCompletionSource = null;
                }
            }
        }

        public static UIModal CreateUIElement(bool cached)
        {
            if (cached)
                Instance ??= new();
            else
                Instance = new();
            if (Instance.Visible)
                ModalInstance?.Shown();
            return Instance;
        }

        public static async Task Show()
        {
            if (ModalVisible)
                ModalVisible = false;

            await Interface.WaitModal();

            Instance ??= new();
            ModalVisible = true;
        }

        public static async Task<TResult> ShowDialog()
        {
            await Show();
            return await Task;
        }

        protected virtual void Shown() { }
        protected virtual void Hidden() { }

        public Modal()
        {
            Instance = this as TModal;

            Top = new(0, .5f, -.5f);
            Left = new(0, .5f, -.5f);

            Visible = ModalVisible;
        }

        protected static void ReturnResult(TResult result)
        {
            taskCompletionSource?.SetResult(result);
            ModalVisible = false;
        }
    }
}
