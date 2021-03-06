using System;
using SimpleLang.Visitors;
using System.Linq;
using System.Collections.Generic;

namespace SimpleLang.ThreeCodeOptimisations
{
    public class EliminationTranToTranOpt : ThreeCodeOptimiser
    {
        public EliminationTranToTranOpt() { }

        private bool _apply = false;
        public bool Applyed()
        {
            return _apply;
        }
        public bool NeedFullCode() { return true; }
        public void Apply(ref List<LinkedList<ThreeCode>> res)
        {
            //throw new NotImplementedException();
            _apply = false;
            LinkedList<ThreeCode> program = new LinkedList<ThreeCode>();
            foreach (LinkedList<ThreeCode> block in res)
            {
                foreach (ThreeCode code in block)
                {
                    program.AddLast(code);
                }
            }
            program = TranToTranOpt(program);
        }

        public void Apply(ref LinkedList<ThreeCode> program)
        {
            _apply = false;
            program = TranToTranOpt(program);
        }

        private List<LinkedListNode<ThreeCode>> FindGotoNodes(LinkedList<ThreeCode> code)
        {
            var currentNode = code.First;
            var gotoNodes = new List<LinkedListNode<ThreeCode>>();
            var res = new List<LinkedListNode<ThreeCode>>();
            var targetLabels = new Dictionary<string, int>();

            while (currentNode != null)
            {
                var currentValue = currentNode.Value;

                if (currentValue.operation == ThreeOperator.Goto)
                {
                    var tacGoto = currentValue;
                    var label = tacGoto.arg1.ToString();
                    if (targetLabels.ContainsKey(label))
                        targetLabels[label]++;
                    else targetLabels.Add(label, 1);

                    gotoNodes.Add(currentNode);
                }

                currentNode = currentNode.Next;
            }

            var usingTargets = new HashSet<string>(targetLabels
               .Where(x => x.Value == 1)
               .Select(x => x.Key));

            foreach (var node in gotoNodes)
            {
                var gotoNode = node.Value;
                if (usingTargets.Contains(gotoNode.arg1.ToString()))
                    res.Add(node);
            }

            return gotoNodes;
        }

        public LinkedListNode<ThreeCode> FindLabel(LinkedList<ThreeCode> code, string lbl)
        {
            var currentNode = code.First;
            while (currentNode != null)
            {
                var line = currentNode.Value;

                if (Equals(line.label, lbl))
                {
                    return currentNode;
                }
                currentNode = currentNode.Next;
            }
            return null;
        }

        public LinkedListNode<ThreeCode> ConvertGotoToIfGotoWithoutLabel(LinkedListNode<ThreeCode> IfGoto)
        {
            var res = IfGoto;
            res.Value.label = null;
            return res;
        }

        public LinkedList<ThreeCode> TranToTranOpt(LinkedList<ThreeCode> code)
        {
            var currentNode = code.First;
            var gotoNodes = FindGotoNodes(code);
            LinkedListNode<ThreeCode> temp;

            while (currentNode != null)
            {
                var line = currentNode.Value;

                if (line.operation is ThreeOperator.Goto)
                {
                    if (line.arg1 != null)
                        temp = FindLabel(code, line.arg1.ToString());
                    else
                        continue;
                    if (temp.Value.operation is ThreeOperator.Goto)
                    {
                        line.arg1 = temp.Value.arg1;
                        _apply = true;
                    }
                    else if (temp.Value.operation is ThreeOperator.None && temp.Next != null && temp.Next.Value.operation is ThreeOperator.Goto)
                    {
                        line.arg1 = temp.Next.Value.arg1;
                        _apply = true;
                    }
                }

                if (line.operation is ThreeOperator.IfGoto)
                {
                    if (line.arg2 != null)
                        temp = FindLabel(code, line.arg2.ToString());
                    else
                        continue;
                    if (temp.Value.operation is ThreeOperator.Goto)
                    {
                        line.arg1 = temp.Value.arg1;
                        _apply = true;
                    }
                    else if (temp.Value.operation is ThreeOperator.None && temp.Next != null && temp.Next.Value.operation is ThreeOperator.Goto)
                    {
                        line.arg1 = temp.Next.Value.arg1;
                        _apply = true;
                    }
                }

                currentNode = currentNode.Next;
            }

            foreach (var gotoNode in gotoNodes)
            {
                var gotoValue = gotoNode.Value;
                var gotoNode2 = gotoNode;

                var targetNode = FindLabel(code, gotoValue.arg1.ToString());
                if (targetNode == null || targetNode.Value.operation != ThreeOperator.IfGoto)
                    continue;

                var nextNode = targetNode.Next;
                if (nextNode == null || nextNode.Value.label == "")
                    continue;
                
                gotoNode2 = ConvertGotoToIfGotoWithoutLabel(targetNode);
                code.Find(gotoNode.Value).Value = gotoNode2.Value;
                var label = new ThreeAddressStringValue(nextNode.Value.label);
                code.AddAfter(gotoNode2, new ThreeCode("", ThreeOperator.Goto, label));
                code.Remove(targetNode);
                _apply = true;
            }

            return code;
        }
    }
}
