using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DAL.DBML;
using System.Text.RegularExpressions;

public partial class moscotadmin_mss_retail_exchange_form : System.Web.UI.Page
{
    /// <summary>
    /// Gets or sets the sub orders.
    /// </summary>
    /// <value>The sub orders.</value>
    public List<mss_RetailOrdersSubMetaDeta> SubOrders
    {
        get
        {
            if (!(Session["SubOrders"] is List<mss_RetailOrdersSubMetaDeta>))
            {
                Session["SubOrders"] = new List<mss_RetailOrdersSubMetaDeta>();
            }
            return (List<mss_RetailOrdersSubMetaDeta>)Session["SubOrders"];
        }
    }
    
    protected void Page_Load(object sender, EventArgs e)
    {
    }

    /// <summary>
    /// Handles the SelectedIndexChanged event of the ProductDropDownList control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    protected void ProductDropDownList_SelectedIndexChanged(object sender, EventArgs e)
    {
        var productDropDownList = sender as DropDownList;

        int productID = Convert.ToInt32((sender as DropDownList).SelectedValue);
        populateColors(productID);
    }

    /// <summary>
    /// Handles the SelectedIndexChanged event of the ColorDropDownList control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    protected void ColorDropDownList_SelectedIndexChanged(object sender, EventArgs e)
    {   
        var colorDropDownList = sender as DropDownList;     

        int productID = Convert.ToInt32(ProductDropDownList.SelectedValue);
        populateSizes(productID, colorDropDownList.SelectedItem.Text);
    }

    /// <summary>
    /// Handles the SelectedIndexChanged event of the SizeDropDownList control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    protected void SizeDropDownList_SelectedIndexChanged(object sender, EventArgs e)
    {
        var sizeDropDownList = sender as DropDownList;

        // Prescription and Lens Items View.
        int typeID = Convert.ToInt32(Request.QueryString["TypeHiddenField"]);
        if (typeID == 33)
        {
            int productID = Convert.ToInt32(ProductDropDownList.SelectedValue);
            PrescriptionView(sizeDropDownList.SelectedItem.Text);
        }
    }

    protected void SizeDropDownList_PreRender(object sender, EventArgs e)
    {
        int repeaterItemIndex = Convert.ToInt32(Request.QueryString["SectionIndex"]);
        int editIndex = Convert.ToInt32(Request.QueryString["EditIndex"]);
        var sizeDropDownList = sender as DropDownList;
        var item = sizeDropDownList.Items.FindByText(GetSubOrders(repeaterItemIndex).ElementAt(editIndex).Size);
        if (item != null)
        {
            item.Selected = true;
        }
    }

    /// <summary>
    /// Handles the SelectedIndexChanged event of the FrameTypeDropDownList control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    protected void FrameTypeDropDownList_SelectedIndexChanged(object sender, EventArgs e)
    {
        var dropDownList = sender as DropDownList;
        
        // With prescription lens 
        PrescriptionPanel.Visible = dropDownList.SelectedValue == "2";
    }

    /// <summary>
    /// Handles the SelectedIndexChanged event of the PrescriptionDropDownList control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    protected void PrescriptionDropDownList_SelectedIndexChanged(object sender, EventArgs e)
    {
        var lensDropDownList = sender as DropDownList;

        if (lensDropDownList.SelectedValue != "0")
        {
            // Update Notes with prescription details.
            int combinationID = Convert.ToInt32(lensDropDownList.SelectedValue);
            var prescriptionPanel = lensDropDownList.Parent as Panel;

            // Visible ODAP & OSAP
            using (var ctx = new MoscotDataClassesDataContext())
            {
                double sku = ctx.mss_ProductAttributeCombinations.FirstOrDefault(p => p.CombinationId == combinationID).Sku;
                ODAPDropDownList.Visible = sku == 7000000004 || sku == 7000000005;
                OSAPDropDownList.Visible = sku == 7000000004 || sku == 7000000005;
            }
        }
    }


    /// <summary>
    /// Populates the sizes.
    /// </summary>
    /// <param name="productID">The product ID.</param>
    /// <param name="color">The color.</param>
    /// <param name="gridView">The grid view.</param>
    /// <param name="repeaterItemIndex">Index of the repeater item.</param>
    private void populateSizes(int productID, string color)
    {
        DAL.DBML.MoscotDataClassesDataContext ctx = new DAL.DBML.MoscotDataClassesDataContext();
        var productSizes = from pacd in ctx.mss_ProductAttributeCombinationDetails
                           join pa in ctx.mss_ProductAttributes on pacd.AttributeId equals pa.AttributeId
                           join pac in ctx.mss_ProductAttributeCombinations on pacd.CombinationId equals pac.CombinationId
                           join pacd2 in ctx.mss_ProductAttributeCombinationDetails on pac.CombinationId equals pacd2.CombinationId
                           join pa2 in ctx.mss_ProductAttributes on pacd2.AttributeId equals pa2.AttributeId
                           join pac2 in ctx.mss_ProductAttributeCombinations on pacd2.CombinationId equals pac2.CombinationId
                           where pa.ClassId == 2 && pa.ProductId == productID &&
                                 pa2.ClassId == 1 && pa2.AttributeValue == color
                           select new
                           {
                               CombinationID = pac.CombinationId,
                               Size = pa.AttributeValue
                           };

        SizeDropDownList.Items.Clear();
        SizeDropDownList.Items.Add(new ListItem()
        {
            Text = "Size",
            Value = "0"
        });

        SizeDropDownList.DataSource = productSizes;
        SizeDropDownList.DataTextField = "Size";
        SizeDropDownList.DataValueField = "CombinationID";
        SizeDropDownList.DataBind();
    }

    private void SelectProduct(string typeID, string type, string productID, string productName, string color, string combinationID, string size, string colorLens, string rxLens, string rxLensID, string rxLensDetails, string amount)
    {
        var ctx = new MoscotDataClassesDataContext();
        if (TypesDropDownList.Items.Count == 0)
        {
            return;
        }


        // If it is not lens type.
        TypesDropDownList.Items.FindByText(type).Selected = true;
        ProductDropDownList.Items.FindByValue(productID.ToString()).Selected = true;

        // For lens types.li
        if (typeID == "33")
        {
            PrescriptionView(size);
            if (!string.IsNullOrEmpty(rxLensDetails))
            {
                // Select Priscription Descriptions.
                SelectPriscriptionDescriptions(rxLensDetails);
            }
        }

        populateColors(Convert.ToInt32(productID));
        ColorDropDownList.Items.FindByText(color).Selected = true;

        populateSizes(Convert.ToInt32(productID), color);
            
        if (string.IsNullOrEmpty(rxLensDetails))
        {
            var productAttributeCombination = ctx.mss_ProductAttributeCombinations.FirstOrDefault(c => c.CombinationId == Convert.ToInt32(rxLensID));
            if (productAttributeCombination != null)
            {
                FrameTypeDropDownList.SelectedValue = productAttributeCombination.Sku.ToString();
            }
        }
        else
        {
            FrameTypeDropDownList.SelectedValue = "2";
            PrescriptionPanel.Visible = true;

            PrescriptionDropDownList.SelectedValue = rxLensID.ToString();

            // Shows.. oDAPDropDownList && oSAPDropDownList
            PrescriptionDropDownList_SelectedIndexChanged(PrescriptionDropDownList, null);

            // Select Priscription Descriptions.
            SelectPriscriptionDescriptions(rxLensDetails);
        }
    }

    private void SelectPriscriptionDescriptions(string sizes)
    {
        string[] headers = new Regex("<br />").Split(sizes);

        // Right instructions.
        string[] right = new Regex(",").Split(headers[0]);
        RXRightDropDownList.SelectedValue = right[0].Replace("Right:", string.Empty);
        ODCYDropDownList.SelectedValue = right[1];
        ODAXDropDownList.SelectedValue = right[2];
        if (ODAPDropDownList.Visible)
        {
            ODAPDropDownList.SelectedValue = right[3];
            if (right.Length > 4)
            {
                ODPrism.Text = right[4];
                ODBC.Text = right[5];
                ODOC.Text = right[6];
                ODSegHT.Text = right[7];
            }
        }
        else
        {
            if (right.Length > 4)
            {
                ODPrism.Text = right[3];
                ODBC.Text = right[4];
                ODOC.Text = right[5];
                ODSegHT.Text = right[6];
            }
        }

        // Left instructions.
        string[] left = new Regex(",").Split(headers[1]);
        OSSPOptionDropDownList.SelectedValue = left[0].Replace("Left:", string.Empty);
        OSCYDropDownList.SelectedValue = left[1];
        OSAXDropDownList.SelectedValue = left[2];
        if (OSAPDropDownList.Visible)
        {
            OSAPDropDownList.SelectedValue = left[3];
            if (left.Length > 4)
            {
                OSPrism.Text = left[4];
                OSBC.Text = left[5];
                OSOC.Text = left[6];
                OSSegHT.Text = left[7];
            }
        }
        else
        {
            if (left.Length > 4)
            {
                OSPrism.Text = left[3];
                OSBC.Text = left[4];
                OSOC.Text = left[5];
                OSSegHT.Text = left[6];
            }
        }

        // PD instuctions.
        PDDropDownList.SelectedValue = headers[2].Replace("PD:", string.Empty);
    }

    /// <summary>
    /// Populates the colors.
    /// </summary>
    /// <param name="productID">The product ID.</param>
    /// <param name="gridView">The grid view.</param>
    /// <param name="repeaterItemIndex">Index of the repeater item.</param>
    private void populateColors(int productID)
    {
        var ctx = new DAL.DBML.MoscotDataClassesDataContext();
        var productColors = (from pacd in ctx.mss_ProductAttributeCombinationDetails
                             join pa in ctx.mss_ProductAttributes on pacd.AttributeId equals pa.AttributeId
                             join pac in ctx.mss_ProductAttributeCombinations on pacd.CombinationId equals pac.CombinationId
                             where pa.ClassId == 1 && pa.ProductId == productID
                             select new
                             {
                                 //CombinationID = pac.CombinationId,
                                 Color = pa.AttributeValue
                             }).Distinct();

        ColorDropDownList.Items.Clear();
        ColorDropDownList.Items.Add(new ListItem()
        {
            Text = "Color",
            Value = ""
        });

        ColorDropDownList.DataSource = productColors;
        ColorDropDownList.DataTextField = "Color";
        ColorDropDownList.DataValueField = "Color";
        ColorDropDownList.DataBind();
    }

    private void PrescriptionView(string prescriptionType)
    {
        switch (prescriptionType)
        {
            case "Progressive":
                ProgressivePrescriptionLenses();
                break;
            case "Single Vision":
                SingleVisionPrescriptionLenses();
                break;
            default:
                //    ClearAntiReflectiveLenses(gridView);
                //SpiritCR39SunLenses(gridView);
                OriginalsGlassSunLenses();
                break;
        }
    }

    private void SpiritCR39SunLenses()
    {
        ColorDropDownList.Visible = true;

        // Priscription hide
        SelectPriscriptionView(false, false);
    }

    private void SingleVisionPrescriptionLenses()
    {
        ColorDropDownList.Visible = true;

        SelectPriscriptionView(false, true);
    }

    private void SelectPriscriptionView(bool p, bool p2)
    {
        // Priscription Panel
        PrescriptionPanel.Visible = p2;

        // odap and osap.
        ODAPDropDownList.Visible = p;
        OSAPDropDownList.Visible = p;
    }

    private void ProgressivePrescriptionLenses()
    {
        ColorDropDownList.Visible = true;

        SelectPriscriptionView(true, true);
    }

    private void OriginalsGlassSunLenses()
    {
        ColorDropDownList.Visible = true;
        SelectPriscriptionView(false, false);
    }

    private void ClearAntiReflectiveLenses()
    {
        ColorDropDownList.Visible = false;

        // Priscription hide
        SelectPriscriptionView(false, false);
    }

    /// <summary>
    /// Gets the sub orders.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns></returns>
    protected IEnumerable<mss_RetailOrderSubsItemMetaDeta> GetSubOrders(int index)
    {
        return SubOrders[index].mss_RetailOrderSubsItemMetaDetas.Where(p => !p.OriginalCombinationID.HasValue);
    }

    /// <summary>
    /// Handles the Selecting event of the ProductsLinqDataSource control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.Web.UI.WebControls.LinqDataSourceSelectEventArgs"/> instance containing the event data.</param>
    protected void ProductsLinqDataSource_Selecting(object sender, LinqDataSourceSelectEventArgs e)
    {
        PopulateProducts(ref e);
    }

    private void PopulateProducts(ref LinqDataSourceSelectEventArgs e)
    {
        int typeID = Convert.ToInt32(e.WhereParameters["TypeID"]);
        var ctx = new MoscotDataClassesDataContext();
        var products = from p in ctx.mss_Products
                       join i in ctx.mss_Indexes on p.ProductID equals i.ProductID
                       orderby p.ProductName
                       where i.TypeID == typeID
                       select new
                       {
                           p.ProductID,
                           p.ProductName
                       };
        e.Result = products;
    }

    protected void ProductDropDownList_PreRender(object sender, EventArgs e)
    {
    }

    protected void UpdateButton_Click(object sender, EventArgs e)
    {
        if (ProductDropDownList.SelectedValue == "0")
        {
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "clientScript", "alert('Please select product.')", true);
            ColorDropDownList.Focus();
            return;
        }
        if (ColorDropDownList.SelectedValue == "")
        {
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "clientScript", "alert('Please select Color.')", true);
            ColorDropDownList.Focus();
            return;
        }
        if (SizeDropDownList.SelectedValue == "0")
        {
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "clientScript", "alert('Please select Size.')", true);
            SizeDropDownList.Focus();
            return;
        }
        decimal productValue = 0m;
        if (!Decimal.TryParse(ProductCostTextBox.Text, out productValue))
        {
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "clientScript", "alert('Please enter correct product cost.')", true);
            ProductCostTextBox.Focus();
            return;
        }


        int typeIndex = Convert.ToInt32(Request.QueryString["typeIndex"]);
        int editIndex = Convert.ToInt32(Request.QueryString["editIndex"]);
        var orderItem = GetSubOrders(typeIndex).ElementAt(editIndex);

        orderItem.ExchangeTypeID = Convert.ToInt32(TypesDropDownList.SelectedValue);
        orderItem.ExchangeCombinationID = Convert.ToInt32(SizeDropDownList.SelectedValue);
        orderItem.ExchangeLensID = Convert.ToInt32(LensDropDownList.SelectedValue);
        orderItem.ExchangePrescriptionLensID = Convert.ToInt32(PrescriptionDropDownList.SelectedValue);
        orderItem.ExchangeProductValue = productValue;

        string description = "";
        if (TypesDropDownList.SelectedValue != "" && TypesDropDownList.SelectedValue != "0")
        {
            description = description + TypesDropDownList.SelectedItem.Text;
        }
        if (ProductDropDownList.SelectedValue != "" && ProductDropDownList.SelectedValue != "0")
        {
            description =  description + ", " + ProductDropDownList.SelectedItem.Text;
        }
        if (ColorDropDownList.SelectedValue != "" && ColorDropDownList.SelectedValue != "0")
        {
            description = description + " - " + ColorDropDownList.SelectedItem.Text;
        }
        if (SizeDropDownList.SelectedValue != "" && SizeDropDownList.SelectedValue != "0")
        {
            description = description + " - " + SizeDropDownList.SelectedItem.Text;
        }
        if (LensDropDownList.SelectedValue != "" && LensDropDownList.SelectedValue != "0")
        {
            description = description + "<br />Lens: " + LensDropDownList.SelectedItem.Text;
        }
        if (PrescriptionDropDownList.SelectedValue != "" && PrescriptionDropDownList.SelectedValue != "0")
        {
            description = description + "<br />RX: " + PrescriptionDropDownList.SelectedItem.Text;
            UpdatePrescription(ref PrescriptionPanel, ref orderItem);
            description = description + "<br />" + orderItem.ExchangePrescriptionLensSizes;
        }

        orderItem.ExchangeData = string.Format("{0}:{1};{2}:{3};{4}:{5};{6}:{7};{8}:{9};{10}:{11};{12}:{13}", TypesDropDownList.SelectedValue, TypesDropDownList.SelectedItem.Text, ProductDropDownList.SelectedValue, ProductDropDownList.SelectedItem.Text, ColorDropDownList.SelectedValue, ColorDropDownList.SelectedItem.Text, SizeDropDownList.SelectedValue, SizeDropDownList.SelectedItem.Text, LensDropDownList.SelectedValue, LensDropDownList.SelectedItem.Text, PrescriptionDropDownList.SelectedValue, PrescriptionDropDownList.SelectedItem.Text, ProductCostTextBox.Text, orderItem.ExchangePrescriptionLensSizes);
        orderItem.ExchangeProductDescription = "<br/><strong>Exchange with :</strong>" + description;

        NotificationLiteral.Text = "Updated.";
        ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "clientScript", "hideExchangePopupControl();", true);
    }

    /// <summary>
    /// Updates the prescription.
    /// </summary>
    /// <param name="prescriptionPanel">The prescription panel.</param>
    /// <param name="subOrderSub">The sub order sub.</param>
    private void UpdatePrescription(ref Panel prescriptionPanel, ref mss_RetailOrderSubsItemMetaDeta subOrderSub)
    {
        if (prescriptionPanel is Panel)
        {
            // Right instructions.
            string right = string.Format("{0},{1},{2}", RXRightDropDownList.Text, ODCYDropDownList.Text, ODAXDropDownList.Text);
            if (ODAPDropDownList.Visible)
            {
                right = string.Format("{0},{1},{2},{3},{4},{5}", right, ODAPDropDownList.Text, ODPrism.Text, ODBC.Text, ODOC.Text, ODSegHT.Text);
            }
            else
            {
                right = string.Format("{0},{1},{2},{3},{4}", right, ODPrism.Text, ODBC.Text, ODOC.Text, ODSegHT.Text);
            }

            // Left instructions.
            string left = string.Format("{0},{1},{2}", OSSPOptionDropDownList.Text, OSCYDropDownList.Text, OSAXDropDownList.Text);
            if (OSAPDropDownList.Visible)
            {
                left = string.Format("{0},{1},{2},{3},{4},{5}", left, OSAPDropDownList.Text, OSPrism.Text, OSBC.Text, OSOC.Text, OSSegHT.Text);
            }
            else
            {
                left = string.Format("{0},{1},{2},{3},{4}", left, OSPrism.Text, OSBC.Text, OSOC.Text, OSSegHT.Text);
            }

            // PD instuctions.
            string pd = PDDropDownList.Text;

            // Add to notes.
            subOrderSub.ExchangePrescriptionLensSizes = string.Format(" Right:{0}<br />Left:{1}<br />PD:{2}", right, left, pd);
        }
    }

}
