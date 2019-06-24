using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonRank.UI
{
    /// <summary>
    /// Asin 分类树形模型
    /// </summary>
    public class AsinCategoryTreeModel
    {
        public AsinCategoryTreeModel()
        {
            Children = new List<AsinCategoryTreeModel>();
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 级别
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// 子节点
        /// </summary>
        public List<AsinCategoryTreeModel> Children { get; set; }

    }
}
