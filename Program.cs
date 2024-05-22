using System;
using System.Diagnostics;
using System.Text;

class Program
{
    static int Main(string[] args)
    {
        if (args.Count() == 0)
        {
            Console.WriteLine("usage: dotnet run [file_path]");
            return 0;
        }
        Dictionary<string, List<List<string>>> grammar = ParseInput(args[0]);

        string fistNonterm = grammar.First().Key;
        grammar.Add("<$S>", [[fistNonterm]]);

        ElimateSolitaryTerminals(ref grammar);
        LimitNonterminals(ref grammar);
        ReplaceUnitProductions(ref grammar);

        File.WriteAllText("cnf-" + args[0], GrammarToString(ref grammar));
        return 0;
    }

    static string GrammarToString(ref Dictionary<string, List<List<string>>> grammar)
    {
        StringBuilder sb = new StringBuilder();
        var vals = grammar.ToList();
        for (int i = 0; i < vals.Count; i++)
        {
            var item = vals[i];
            sb.Append($"{item.Key} -> ");
            for (int j = 0; j < item.Value.Count; j++)
            {
                foreach (var sym in item.Value[j])
                {
                    sb.Append($"{sym} ");
                }
                if (j != item.Value.Count - 1)
                    sb.Append("| ");
            }
            if (i != vals.Count - 1)
                sb.Append("\n");
        }
        return sb.ToString();
    }
    static Dictionary<string, List<List<string>>> ParseInput(string path)
    {
        string bnfGrammar = File.ReadAllText(path);

        var nonterminals = new Dictionary<string, List<List<string>>>();

        foreach (var line in bnfGrammar.Split("\n"))
        {
            var productions = new List<List<string>>();
            var currentProduction = new List<string>();


            var words = line.Trim().Split(" ");
            var name = words[0];
            if (words[0] == "#" || words.Length == 1)
                continue;

            Debug.Assert(words[1] == "::=");

            foreach (var word in words[2..])
            {
                if (word == "|")
                {
                    Debug.Assert(currentProduction.Count() > 0);

                    productions.Add(new List<string>(currentProduction));
                    currentProduction.Clear();
                    continue;
                }
                currentProduction.Add(word);
            }
            productions.Add(currentProduction);
            nonterminals.Add(name, productions);
        }

        return nonterminals;
    }

    static void ElimateSolitaryTerminals(ref Dictionary<string, List<List<string>>> grammar)
    {
        // nonterminals must be marked with brackets, ie <nonterm>. 
        // terminals require not special marking 

        var terminalToNonterminal = new Dictionary<string, string>();

        foreach (var nonterm in grammar)
        {
            foreach (var production in nonterm.Value)
            {
                for (int i = 0; i < production.Count(); i++)
                {
                    string symbol = production[i];
                    if (symbol.StartsWith('<') && symbol.EndsWith('>')) // is a nonterminal 
                        continue;
                    else if (production.Count == 1)
                        continue;

                    string? converted;
                    if (terminalToNonterminal.TryGetValue(symbol, out converted))
                        production[i] = converted;
                    else
                    {
                        converted = "<$" + symbol + ">";
                        production[i] = converted;
                        terminalToNonterminal.Add(symbol, converted);
                    }
                }
            }
        }
        foreach (var conversion in terminalToNonterminal) // add new productions to grammar 
        {
            grammar.Add(conversion.Value, [[conversion.Key]]);
        }
    }

    static void LimitNonterminals(ref Dictionary<string, List<List<string>>> grammar)
    {
        // since we have already run ElimateSolitaryTerminals(), we know that if production.Count > 1
        // then all elements are nonterminals
        // A -> X_1 X_2 X_3 X_4 
        // becomes 
        // A -> X_1 A1 
        // A1 -> X_2 A2
        // A2 -> X_3 X_4 

        var newNonTerms = new Dictionary<string, List<string>>();
        foreach (var nonterm in grammar)
        {
            for (int i = 0; i < nonterm.Value.Count(); i++)
            {
                if (nonterm.Value[i].Count <= 2)
                    continue;

                var prods = new List<string>(nonterm.Value[i]);
                nonterm.Value[i] = [nonterm.Value[i][0], "<$" + i + "1" + nonterm.Key + ">"];

                for (int j = 1; j < prods.Count() - 1; j++)
                {
                    string newNonTerm = "<$" + i + j + nonterm.Key + ">";
                    var newProd = new List<string>();
                    if (j + 2 == prods.Count())
                        newProd = prods[j..];
                    else
                        newProd = [prods[j], "<$" + i + (j + 1) + nonterm.Key + ">"];
                    newNonTerms.Add(newNonTerm, newProd);
                }
            }
        }
        foreach (var nonterm in newNonTerms)
        {
            grammar.Add(nonterm.Key, [nonterm.Value]);
        }
    }

    static void ReplaceUnitProductions(ref Dictionary<string, List<List<string>>> grammar)
    {
        // A ::= B
        // B ::= X1 X2
        // remove B from productions of A and append X1 X2 
        foreach (var nonterm in grammar) // current nonterminal 
        {
            for (int i = 0; i < nonterm.Value.Count(); i++)
            {
                List<string> prod = nonterm.Value[i];

                if (prod.Count() > 1 || !prod[0].StartsWith("<"))
                    continue;

                var symbol = prod[0];
                List<List<string>> fullProductions = grammar[symbol];


                nonterm.Value.RemoveAt(i);
                i--;

                nonterm.Value.AddRange(fullProductions);
            }
        }
    }
}