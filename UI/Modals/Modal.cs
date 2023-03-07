using Cornifer.UI.Elements;
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
                    ModalInstance?.Shown();
                else
                    ModalInstance?.Hidden();
                
                if (Instance is not null)
                    Instance.Visible = value;

                if (!value && taskCompletionSource is not null)
                {
                    if (!taskCompletionSource.Task.IsCompleted && !taskCompletionSource.Task.IsCanceled)
                        taskCompletionSource.SetCanceled();
                    taskCompletionSource = null;
                }
            }
        }

        public static UIModal CreateUIElement()
        {
            Instance = new();
            if (Instance.Visible)
                ModalInstance?.Shown();
            return Instance;
        }

        public static void Show()
        {
            if (ModalVisible)
                ModalVisible = false;

            Instance ??= new();
            ModalVisible = true;
        }

        public static async Task<TResult> ShowAsync()
        {
            Show();
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
