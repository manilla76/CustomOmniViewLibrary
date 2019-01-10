using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using BlendingOptimizationEngine;

namespace CustomOmniViewLibrary
{
    public class OmniViewCustom : DialogViewModelBase, IDisposable
    {
        private int myProperty;
        private string configurationPath = "Nautilus\\CustomOmniView";
        private string dustGroup;
        private string dustTag;

        public int MyProperty { get => myProperty; set => Set(ref myProperty, value); }
        private int oldDustMode;
        private ObservableCollection<QcModel> qcModifiers = new ObservableCollection<QcModel>();
        public ObservableCollection<QcModel> QcModifiers { get => qcModifiers; set { Set(ref qcModifiers, value); SaveSettingsToConfig(); } }
        public string DustGroup { get => dustGroup; set { Set(ref dustGroup, value); UpdateDustTag();} }

        private void UpdateDustTag()
        {
            if (DustMode != null)
            {
                DustMode.UpdateValueEvent -= DustMode_UpdateValueEvent;
                DustMode.Dispose();
            }
            if (string.IsNullOrEmpty(DustGroup) || string.IsNullOrEmpty(DustTag)) { return; }
            DustMode = Thermo.Datapool.Datapool.DatapoolSvr.CreateTagInfo(DustGroup, DustTag, Thermo.Datapool.Datapool.dpTypes.INT);
            DustMode.UpdateValueEvent += DustMode_UpdateValueEvent;
            oldDustMode = DustMode.AsInt;
            SaveSettingsToConfig();
        }

        public string DustTag { get => dustTag; set { Set(ref dustTag, value); UpdateDustTag(); }
}
        private string fault;
        public string Fault { get => fault; set => Set(ref fault, value); }

        private ThermoArgonautLibrary.Product currentProduct = null;
        private static ThermoArgonautViewerLibrary.Argonaut omniView;
        private static ThermoArgonautLibrary.CommonSystem.ServerItemUpdate serverItemUpdate;
        private static ThermoArgonautViewerLibrary.CommonSystemViewer.BOSCtrl ramos = new ThermoArgonautViewerLibrary.CommonSystemViewer.BOSCtrl(null);
        private ObservableDictionary<int, ThermoArgonautLibrary.Product> products = new ObservableDictionary<int, ThermoArgonautLibrary.Product>();
        private ObservableCollection<DPModel<double>> product1 = new ObservableCollection<DPModel<double>>();
        public ObservableCollection<DPModel<double>> Product1 { get => product1; set => Set(ref product1, value); }
        public ObservableDictionary<int, ThermoArgonautLibrary.Product> Products { get => products; set => Set(ref products, value); }
        private Thermo.Datapool.Datapool.ITagInfo dustMode;
        public Thermo.Datapool.Datapool.ITagInfo DustMode { get => dustMode; set => Set(ref dustMode, value); }
                
        private DPModel<int> testDP = new DPModel<int>();

        public DPModel<int> TestDP { get => testDP; set => Set(ref testDP, value); }

        private DPModel<string> recipeChange;

        public DPModel<string> RecipeChange { get => recipeChange;
            set => Set(ref recipeChange, value); }
        public DPModel<double> setpointChange;
        public DPModel<double> SetpointChange { get => setpointChange;
            set => Set(ref setpointChange, value); }
        
        public OmniViewCustom()
        {
            MyProperty = 7;
            Init();
            AddTestData();
            AddTestProduct();
            //var analysis = omniView.GetCurrentAnalysis();
            omniView = new ThermoArgonautViewerLibrary.Argonaut();
            serverItemUpdate = new ThermoArgonautLibrary.CommonSystem.ServerItemUpdate();
            serverItemUpdate.IsUpdated(ThermoArgonautLibrary.CommonSystem.ServerItemUpdate.ItemsEnum.UpdateProduct, omniView.GetItemUpdate());
            currentProduct = new ThermoArgonautLibrary.Product();
            currentProduct.UpdateEvent += CurrentProduct_UpdateEvent;            
            var currentItemUpdate = omniView.GetItemUpdate();
            ramos.Recipe.UpdateEvent += Recipe_UpdateEvent;
            var recipe = omniView.GetRaMOSRecipe();
            LoadSettingsFromConfig(recipe.Items);
        }

        private void OmniViewCustom_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SaveSettingsToConfig();
        }

        private void LoadSettingsFromConfig(List<BlendingOptimizationEngine.BlendingOptimizationSystem.BlendRecipe.QCCfg> items)
        {
            Thermo.Configuration.Branch branch;
            try
            {
                branch = Thermo.Configuration.Configuration.GetBranch(configurationPath);
            }
            catch
            {
                Thermo.Configuration.Configuration.CreatePath(configurationPath);
                branch = Thermo.Configuration.Configuration.GetBranch(configurationPath);
            }
            var valueNames = branch.GetValueNames("");
            foreach (var item in items)
            {
                var index = QcModifiersContains(item);
                if (index != -1)  // found
                {
                    QcModifiers[index].Modifier = Thermo.Configuration.Configuration.GetDouble(configurationPath, item.QcName, 0);
                    QcModifiers[index].PropertyChanged += OmniViewCustom_PropertyChanged;
                }
                else   // not found
                {
                    if (valueNames.Contains(item.QcName))
                    {
                        QcModifiers.Add(new QcModel { Qc = item.QcName, Modifier = Thermo.Configuration.Configuration.GetDouble(configurationPath, item.QcName, 0) });
                    }
                    else
                    {
                        QcModifiers.Add(new QcModel { Qc = item.QcName, Modifier = 0 });
                    }
                    
                    QcModifiers[QcModifiers.Count - 1].PropertyChanged += OmniViewCustom_PropertyChanged;
                }
            }
            DustGroup = Thermo.Configuration.Configuration.GetString(configurationPath, "DustGroup", "");
            DustTag = Thermo.Configuration.Configuration.GetString(configurationPath, "DustTag", "");
            SaveSettingsToConfig();
            
        }

        private int QcModifiersContains(BlendingOptimizationSystem.BlendRecipe.QCCfg item)
        {
            for (int i = 0; i < QcModifiers.Count; i++)
            {
                if (item.QcName == QcModifiers[i].Qc)
                {
                    return i;
                }
            }
            return -1;
        }

        private void SaveSettingsToConfig()
        {
            Thermo.Configuration.Configuration.CreatePath(configurationPath);

            foreach (var qcmod in QcModifiers) 
            {
                Thermo.Configuration.Configuration.SetValue(configurationPath, qcmod.Qc, qcmod.Modifier);
            }

            Thermo.Configuration.Configuration.SetValue(configurationPath, "DustGroup", DustGroup);
            Thermo.Configuration.Configuration.SetValue(configurationPath, "DustTag", DustTag);
        }

        private void CurrentProduct_UpdateEvent(ThermoArgonautLibrary.Product e)
        {
            UpdateProductList(e);
        }

        private void Init()
        {

            var tag = Thermo.Datapool.Datapool.DatapoolSvr.CreateTagInfo("TestGroup", "Setpoint", Thermo.Datapool.Datapool.dpTypes.FLOAT);
            SetpointChange = new DPModel<double> { Value = tag, Name = "LSF" };
            SetpointChange.PropertyChanged += SetpointChange_PropertyChanged;            
            
            return;
        }

        private void DustMode_UpdateValueEvent(Thermo.Datapool.Datapool.ITagInfo e)
        {
            if (DustMode.AsInt != 2 && oldDustMode ==2)  // change from 2 to 0 or 1
            {
                var recipe = omniView.GetRaMOSRecipe();
                recipe.Items.ForEach((r) => r.Setpoint += GetQcModifier(r.QcName).Modifier);
            
                omniView.SetRecipe(recipe);
            }
            else if (DustMode.AsInt == 2 && oldDustMode != 2)  // Change from 0 or 1 to 2
            {
                var recipe = omniView.GetRaMOSRecipe();
                recipe.Items.ForEach((r) => r.Setpoint -= GetQcModifier(r.QcName).Modifier);
                omniView.SetRecipe(recipe);
            }
            oldDustMode = DustMode.AsInt;
        }

        private QcModel GetQcModifier (string qc)
        {
            foreach (QcModel qcMod in QcModifiers)
            {
                if (qcMod.Qc.ToLower() == qc.ToLower())
                {
                    return qcMod;
                }
            }
                return new QcModel();
         
        }

        private void SetpointChange_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // get current recipe
            // change setpoint....should associate both QC and setpoint (dictionary?)                
            
        }

        private ICommand changeRecipeCommand;
        public ICommand ChangeRecipeCommand => changeRecipeCommand ?? (changeRecipeCommand = new RelayCommand((p) =>
        {
            
        }));

        private ICommand testCommand;
        public ICommand TestCommand => testCommand ?? (testCommand = new RelayCommand((p) =>
        {
            var currentItemUpdate = omniView.GetItemUpdate();
            if (serverItemUpdate.IsUpdated(ThermoArgonautLibrary.CommonSystem.ServerItemUpdate.ItemsEnum.CurrentProduct, currentItemUpdate))
            {
                try
                {
                    var a = omniView.GetCurrentAnalysis();

                    var b = ThermoArgonautLibrary.CommonSystem.UpdateProduct;
                    
                    var c = omniView.GetProductIds();
                    currentProduct.Assign(omniView.GetCurrentProduct());
                }
                catch (System.Exception e)
                {
                    Fault = e.Message;
                }
            }
                

            //Product1[0].SetValue( 100.3 + Product1[0].Value);
        }));

        public void AddTestData()
        {
            var tag = Thermo.Datapool.Datapool.DatapoolSvr.CreateTagInfo("TestGroup", "Recipe", Thermo.Datapool.Datapool.dpTypes.STRING);
            RecipeChange = new DPModel<string> { Value = tag, Name = "RecipeChange" };
            RecipeChange.PropertyChanged += RecipeChange_PropertyChanged;
        }

        private void RecipeChange_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ChangeRecipe(RecipeChange.Value.AsString);            
        }

        private void ChangeRecipe(string recipe)
        {
            if (omniView == null)
            {
                return;
            }
            if (omniView.GetRecipeNameList().Items.Contains(recipe) == false)
            {
                return;
            }
            var ramosConfig = omniView.GetRaMOSConfiguration();
            ramosConfig.RecipeName = recipe;
            omniView.SetRaMOSConfiguration(ramosConfig);
        }

        private void Recipe_UpdateEvent(BlendingOptimizationSystem.BlendRecipe e)
        {

        }

        private void UpdateProductList(ThermoArgonautLibrary.Product product)
        {
            if (Products.ContainsKey(product.ProductId) == false)
            {
                Products.Add(product.ProductId, product);
            }
            else
            {
                Products[product.ProductId] = product;
            }
        }

        public void AddTestProduct()
        {
            //Product1.Add( new DPModel<double> { Name = "tons", Value = 0, ProductName="Sand", HighAlarm = 20000 } );
            //Product1.Add( new DPModel<double> { Name = "CaO", Value = 45, ProductName = "Sand", HighAlarm = 50, LowAlarm = 30 });
            //Product1.Add(new DPModel<double> { Name = "SiO2", Value = 12, ProductName = "Sand", HighAlarm = 15, LowAlarm = 10 });
            //Product1.Add(new DPModel<double> { Name = "Fe2O3", Value = 3.2, ProductName = "Sand", HighAlarm = 5, LowAlarm = 2 });
            //Product1.Add(new DPModel<double> { Name = "tons", Value = 2334, ProductName = "Limestone", HighAlarm = 2000 });
            //Product1.Add(new DPModel<double> { Name = "CaO", Value = 43.4, ProductName = "Limestone", HighAlarm = 50, LowAlarm = 44 });
            //Product1.Add(new DPModel<double> { Name = "SiO2", Value = 13.1, ProductName = "Limestone", HighAlarm = 15, LowAlarm = 10 });
            //Product1.Add(new DPModel<double> { Name = "Fe2O3", Value = 3.6, ProductName = "Limestone", HighAlarm = 5, LowAlarm = 2 });
            var tag = Thermo.Datapool.Datapool.DatapoolSvr.CreateTagInfo("TestGroup", "TestTag", Thermo.Datapool.Datapool.dpTypes.FLOAT);
            Product1.Add(new DPModel<double> { Name = "test", ProductName = "Sand", HighAlarm = 200, LowAlarm = 0, Value = tag });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    recipeChange.Value.Dispose();
                    foreach (var p in Product1)
                    {
                        p.Value.Dispose();
                    }
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~OmniViewCustom() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
