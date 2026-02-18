using Microsoft.AspNetCore.Mvc;

namespace TechWayFit.Pulse.Web.ViewComponents.TreeDropdown
{
    public class TreeDropdownViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(
            string id, 
            string? name = null, 
            string? value = null, 
            string placeholder = "Select an option", 
            IEnumerable<TreeDropdownItem>? items = null, 
            bool allowClear = false, 
            string cssClass = "")
        {
            var model = new TreeDropdownViewModel
            {
                Id = id,
                Name = name ?? id,
                Value = value ?? string.Empty,
                Placeholder = placeholder,
                Items = items ?? Enumerable.Empty<TreeDropdownItem>(),
                AllowClear = allowClear,
                CssClass = cssClass
            };

            return View(model);
        }
    }

    public class TreeDropdownViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Placeholder { get; set; } = "Select an option";
        public IEnumerable<TreeDropdownItem> Items { get; set; } = Enumerable.Empty<TreeDropdownItem>();
        public bool AllowClear { get; set; } = false;
        public string CssClass { get; set; } = string.Empty;
    }

    public class TreeDropdownItem
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? ParentValue { get; set; }
        public int Level { get; set; } = 0;
        public bool HasChildren { get; set; } = false;
        public string Icon { get; set; } = string.Empty;
        public bool IsExpanded { get; set; } = false;
    }
}