﻿using NTMiner.Core;
using NTMiner.Vms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTMiner {
    public partial class AppContext {
        public class SysDicItemViewModels : ViewModelBase {
            private readonly Dictionary<Guid, SysDicItemViewModel> _dicById = new Dictionary<Guid, SysDicItemViewModel>();

            public SysDicItemViewModels() {
                NTMinerRoot.Instance.OnContextReInited += () => {
                    _dicById.Clear();
                    Init();
                };
                NTMinerRoot.Instance.OnReRendContext += () => {
                    OnPropertyChangeds();
                };
                On<SysDicItemAddedEvent>("添加了系统字典项后调整VM内存", LogEnum.DevConsole,
                    action: (message) => {
                        if (!_dicById.ContainsKey(message.Source.GetId())) {
                            _dicById.Add(message.Source.GetId(), new SysDicItemViewModel(message.Source));
                            OnPropertyChangeds();
                            SysDicViewModel sysDicVm;
                            if (Current.SysDicVms.TryGetSysDicVm(message.Source.DicId, out sysDicVm)) {
                                sysDicVm.OnPropertyChanged(nameof(sysDicVm.SysDicItems));
                                sysDicVm.OnPropertyChanged(nameof(sysDicVm.SysDicItemsSelect));
                            }
                        }
                    });
                On<SysDicItemUpdatedEvent>("更新了系统字典项后调整VM内存", LogEnum.DevConsole,
                    action: (message) => {
                        if (_dicById.ContainsKey(message.Source.GetId())) {
                            SysDicItemViewModel entity = _dicById[message.Source.GetId()];
                            int sortNumber = entity.SortNumber;
                            entity.Update(message.Source);
                            if (sortNumber != entity.SortNumber) {
                                SysDicViewModel sysDicVm;
                                if (Current.SysDicVms.TryGetSysDicVm(entity.DicId, out sysDicVm)) {
                                    sysDicVm.OnPropertyChanged(nameof(sysDicVm.SysDicItems));
                                    sysDicVm.OnPropertyChanged(nameof(sysDicVm.SysDicItemsSelect));
                                }
                            }
                        }
                    });
                On<SysDicItemRemovedEvent>("删除了系统字典项后调整VM内存", LogEnum.DevConsole,
                    action: (message) => {
                        _dicById.Remove(message.Source.GetId());
                        OnPropertyChangeds();
                        SysDicViewModel sysDicVm;
                        if (Current.SysDicVms.TryGetSysDicVm(message.Source.DicId, out sysDicVm)) {
                            sysDicVm.OnPropertyChanged(nameof(sysDicVm.SysDicItems));
                            sysDicVm.OnPropertyChanged(nameof(sysDicVm.SysDicItemsSelect));
                        }
                    });
                Init();
            }

            private void Init() {
                foreach (var item in NTMinerRoot.Instance.SysDicItemSet) {
                    _dicById.Add(item.GetId(), new SysDicItemViewModel(item));
                }
            }

            private void OnPropertyChangeds() {
                OnPropertyChanged(nameof(List));
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(KernelBrandItems));
            }

            public List<SysDicItemViewModel> KernelBrandItems {
                get {
                    List<SysDicItemViewModel> list = new List<SysDicItemViewModel> {
                    SysDicItemViewModel.PleaseSelect
                };
                    SysDicViewModel sysDic;
                    if (Current.SysDicVms.TryGetSysDicVm("KernelBrand", out sysDic)) {
                        list.AddRange(List.Where(a => a.DicId == sysDic.Id).OrderBy(a => a.SortNumber));
                    }
                    return list;
                }
            }

            public int Count {
                get {
                    return _dicById.Count;
                }
            }

            public bool TryGetValue(Guid id, out SysDicItemViewModel vm) {
                return _dicById.TryGetValue(id, out vm);
            }

            public List<SysDicItemViewModel> List {
                get {
                    return _dicById.Values.ToList();
                }
            }
        }
    }
}
