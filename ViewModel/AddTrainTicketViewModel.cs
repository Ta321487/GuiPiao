using CommunityToolkit.Mvvm.ComponentModel;
using GuiPiao.Model;
using GuiPiao.View;
using GuiPiao.ViewModel.TrainTicketForm;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace GuiPiao.ViewModel
{
    /// <summary>
    /// 添加火车票 ViewModel
    /// </summary>
    public partial class AddTrainTicketViewModel : TrainTicketFormViewModelBase
    {
        /// <summary>
        /// 是否已成功保存（用于改签逻辑判断）
        /// </summary>
        public bool IsSaved { get; private set; } = false;

        /// <summary>
        /// 是否跳过加载默认值（用于改签模式）
        /// </summary>
        public bool SkipLoadDefaults { get; set; } = false;

        public AddTrainTicketViewModel()
        {
            WindowTitle = "添加火车票";
            SaveButtonText = "保存";
            IsEditMode = false;
            IsStatusVisible = true; // 新增窗口显示状态下拉框
        }

        /// <summary>
        /// 初始化加载默认值（在 SkipLoadDefaults 设置后调用）
        /// </summary>
        public void InitializeDefaults()
        {
            // 加载默认值（除非在改签模式下）
            if (!SkipLoadDefaults)
            {
                LoadDefaultValues();
            }
        }

        /// <summary>
        /// 应用改签数据
        /// </summary>
        public async Task ApplyRescheduleDataAsync(string departStation, string arriveStation, bool isChangeDestination)
        {
            _isApplyingRescheduleData = true;
            try
            {
                // 设置只读状态
                IsDepartStationReadOnly = true;
                IsArriveStationReadOnly = !isChangeDestination;

                // 先同步到 FormData，确保数据不丢失
                _formData.DepartStationInput = departStation;
                _formData.ArriveStationInput = arriveStation;

                // 设置车站值（这会触发属性变更通知）
                DepartStationInput = departStation;
                ArriveStationInput = arriveStation;

                // 查询并设置车站代码（外键约束需要）
                await QueryDepartStationInfoAsync();
                await QueryArriveStationInfoAsync();

                // 改签状态默认为已完成，并隐藏状态下拉框
                SelectedStatus = "已完成";
                _formData.SelectedStatus = "已完成";
                IsStatusVisible = false;

                // 清空标签选择（新车票不继承原票标签）
                SelectedTagIds.Clear();
                _formData.SelectedTagIds.Clear();

                // 触发属性变更通知，确保UI更新
                OnPropertyChanged(nameof(DepartStationInput));
                OnPropertyChanged(nameof(ArriveStationInput));
                OnPropertyChanged(nameof(DepartStationCode));
                OnPropertyChanged(nameof(ArriveStationCode));
                OnPropertyChanged(nameof(IsDepartStationReadOnly));
                OnPropertyChanged(nameof(IsArriveStationReadOnly));
                OnPropertyChanged(nameof(SelectedStatus));
                OnPropertyChanged(nameof(IsStatusVisible));
                OnPropertyChanged(nameof(SelectedTagIds));
            }
            finally
            {
                _isApplyingRescheduleData = false;
            }
        }

        /// <summary>
        /// 验证表单数据（改签模式下需要验证改签类型）
        /// </summary>
        protected override bool ValidateForm()
        {
            // 先执行基类验证
            if (!base.ValidateForm())
                return false;

            // 改签模式下，验证改签类型是否填写
            if (IsRescheduleMode && string.IsNullOrWhiteSpace(TicketModificationType))
            {
                MessageBoxWindow.Show("请选择改签类型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 执行保存操作 - 添加新车票
        /// </summary>
        protected override async Task ExecuteSaveAsync()
        {
            if (!ValidateForm())
                return;

            var trainRide = CreateTrainRideInfo();

            try
            {
                // 添加车票并获取新ID
                int newId = await _trainRideRepository.AddTrainRideAsync(trainRide);
                
                // 保存标签关联
                await SaveTagsAsync(newId);
                
                // 标记已保存
                IsSaved = true;
                
                // 重要：在显示成功消息和关闭窗口之前，先重置未保存更改标志
                // 这样关闭窗口时不会再次弹出确认对话框
                HasUnsavedChanges = false;
                
                LogSaveOperation("添加");
                ShowSaveSuccessAndClose("火车票添加成功");
            }
            catch (Exception ex)
            {
                ShowSaveError("添加", ex.Message);
            }
        }
    }
}
