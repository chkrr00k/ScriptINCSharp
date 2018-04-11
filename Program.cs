//by chkrr00k GNU GPLv3 license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text.RegularExpressions;

namespace ConsoleApplication60 {

    abstract class Symbol {
        //Describes all objects that can be parsed and evaluated;

        //Factory pattern;
        public static Symbol of(string input) {
            double numberVal;

            input = input.Trim();
            if(Regex.IsMatch(input, Variable.VARIABLE)) {
                return new Variable(input);
            } else if(input.IndexOfAny(new[] { '+','-','*','/' }) > -1) {
                return new Operand(input);
            } else if(double.TryParse(input, out numberVal)) {
                return new Number(numberVal);
            } else {
                throw new ArgumentException("Invalid symbol " + input);
            }
        }
    }

    class Operand: Symbol {
        private string type;
        //Describes an operator such as +, -, * and /;
        public Operand(string type) {
            if(type.Trim().IndexOfAny(new[] { '+', '-', '*', '/' }) < 0) {
                throw new ArgumentException("You can only use + - * / as type. Error in " + type);
            }
            this.type = type.Trim();
        }

        //returns the operator itself for the dt parser;
        public override string ToString() {
            return this.type;
        }
    }

    class Variable: Symbol {
        private string varName;
        private string varValue;
        public const string VARIABLE = "^\\$[A-Z]+$";

        //Descrive a textual string in the pattern of $NAME that have a numerical value in the environment;
        public Variable(string name) {
            if(!Regex.IsMatch(name.Trim(), Variable.VARIABLE)) {
                throw new ArgumentException("Variable name can only starts witn $, must not contains numbers and must be UPPERCASE. Error in " + varName);
            }else{
                this.varName = name.Trim();
                this.varValue = "0";
            }
        }

        public string value { set { this.varValue = value; } }
        public string name { get { return this.varName; } }

        //returns the value of the variable for the dt parser. If it wasn't setted by the caller default is "0" (string zero);
        public override string ToString() {
            return this.varValue;
        }
    }

    class Number: Symbol {
        private double value;

        //Describe a real number in the standard C# format.
        public Number(double number) {
            this.value = number;
        }

        //returns the number itself for the dt parser in string form;
        public override string ToString() {
            return this.value.ToString();
        }
    }

    class Environment {
        private Dictionary<string, string> variables;

        //Describes all possible settings / variables value that the user can set;
        public Environment() {
            this.variables = new Dictionary<string, string>();
        }

        //Add a variable;
        public void registerVariable(string name, string value) {
            if(!Regex.IsMatch(name, Variable.VARIABLE)) {
                throw new ArgumentException("Variable name can only starts witn $, must not contains numbers and must be UPPERCASE. Error in " + name);
            } else {
                this.variables.Add(name, value);
            }
            
        }
        //Remove a variable;
        public void unregisterVariable(string name, string value) {
            this.variables.Remove(name);
        }
        //Get the variable value;
        public string getVariableValue(string name) {
            if(this.variables.ContainsKey(name)) {
                return this.variables[name];
            }else {
                throw new KeyNotFoundException();
            }
        }

    }

    class Line {
        private List<Symbol> symbols;
        bool resolved;

        //Describes a line that need evaluation;
        public Line(string input) {
            this.symbols = new List<Symbol>();
            this.resolved  = false;
            // 1) tokenize string with space as separator;
            // 2) convert into list;
            // 3) for each add them into the initialized list (with the factory);
            input.Trim().Split(' ').ToList().ForEach(s => this.symbols.Add(Symbol.of(s)));
        }

        //Resolve all eventual variable name;
        public void resolve(Environment env) {
            if(!this.resolved) {
                // 1) filter all symbols to find all Variable;
                // 2) casts them as Variable because they were Symbols;
                // 3) convert it as a List to call the ForEach;
                // 4) for each Variable set its value as the environment say its the proper value
                // /!\ IF VARABLE IS MISSING EXCEPTION IS THROWN HERE   
                this.symbols.FindAll(s => s is Variable).Cast<Variable>().ToList().ForEach(v => v.value = env.getVariableValue(v.name));
                this.resolved = true;
            }
        }

        //Return the line to evaluate for the evaulator;
        public string getEvalLine() {
            if(!this.resolved) {
                throw new ArgumentException("Eventual variable names MUST be resolved");
            } else {
                // use the overridden ToString();
                return string.Join("", this.symbols);
            }
        }

        //To avoid to call two functions instead of one;
        public string getEvalLine(Environment env) {
            this.resolve(env);
            return this.getEvalLine();
        }

        public override string ToString() {
            return string.Join("", this.symbols);
        }
    }

    class Evaluator {
        private Environment env;
        private DataTable dt;

        //The evaluator itself!;
        public Evaluator(Environment env) {
            this.env = env;
            //Mystery magical component;
            dt = new DataTable();
        }
        public Evaluator(): this(new Environment()) { }

        public Environment environment { get { return this.env; } set { this.env = value; } }

        public string evaluate(Line line) {
            try {
                //Calls the Computed method on the getEvalLine of the passed line. NOTE THAT EVENTUAL VARIABLES ARE RESOLVED!;
                return this.dt.Compute(line.getEvalLine(this.env), "").ToString();
            } catch(KeyNotFoundException e) {
                throw e;
            }
        }
    }

 

    class Program {
        static void Main(string[] args) {
            string tmp;
            Environment e = new Environment();
            e.registerVariable("$ONE", "2");
            Evaluator eval = new Evaluator(e);
            tmp = eval.evaluate(new Line("2 + $ONE"));
            e.registerVariable("$TWO", tmp);
            Console.WriteLine(eval.evaluate(new Line("6 - $TWO")));
            Console.ReadKey();
        }
    }
}
