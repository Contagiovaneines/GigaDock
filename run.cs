using System;
using System.IO;
using System.Text.RegularExpressions;

string file = "c:\\Users\\giovane\\Documents\\dockwindows\\src\\DockWindows.App\\Views\\AjustesWindow.xaml";
string text = File.ReadAllText(file);

string pattern = @"(?s)<!-- 4\. APAR.*?<!-- 5\. PAINEL DE EDIÇÃO GERAL";
string replacement = "".Replace("\"", "") + "\n                    <!-- ================================================= -->\n                    <!-- 5. PAINEL DE EDIÇÃO GERAL";

string result = Regex.Replace(text, pattern, replacement);

File.WriteAllText(file, result);
