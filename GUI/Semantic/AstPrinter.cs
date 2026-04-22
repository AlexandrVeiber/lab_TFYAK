using System.Text;

namespace GUI.Semantic
{
    public static class AstPrinter
    {
        public static string Print(AstNode root)
        {
            if (root == null)
                return "AST не построено.";

            var sb = new StringBuilder();
            AppendNode(sb, root, "", true, null);
            return sb.ToString();
        }

        private static void AppendNode(
            StringBuilder sb,
            AstNode node,
            string indent,
            bool isLast,
            string? label)
        {
            if (string.IsNullOrEmpty(indent) && label == null)
            {
                sb.AppendLine(node.NodeType);
            }
            else
            {
                sb.Append(indent);
                sb.Append(isLast ? "└── " : "├── ");

                if (!string.IsNullOrEmpty(label))
                {
                    sb.Append(label);
                    sb.Append(": ");
                }

                sb.AppendLine(node.NodeType);
            }

            var properties = node.GetProperties();
            var children = node.GetChildren();

            int totalItems = properties.Count + children.Count;

            string childIndent;
            if (string.IsNullOrEmpty(indent) && label == null)
                childIndent = "";
            else
                childIndent = indent + (isLast ? "    " : "│   ");

            int currentIndex = 0;

            foreach (var property in properties)
            {
                bool propertyIsLast = currentIndex == totalItems - 1;

                sb.Append(childIndent);
                sb.Append(propertyIsLast ? "└── " : "├── ");
                sb.Append(property.Name);
                sb.Append(": ");
                sb.AppendLine(property.Value);

                currentIndex++;
            }

            foreach (var child in children)
            {
                bool childIsLast = currentIndex == totalItems - 1;
                AppendNode(sb, child.Node, childIndent, childIsLast, child.Label);
                currentIndex++;
            }
        }
    }
}