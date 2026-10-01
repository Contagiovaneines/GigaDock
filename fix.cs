using System;
using System.IO;
using System.Text.RegularExpressions;

string path = @"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\ColecaoAppViewModel.cs";
string text = File.ReadAllText(path);

text = text.Replace("public ICommand ExecutarItemCommand { get; }", "public ICommand ExecutarItemCommand { get; }\n    public ICommand RemoverColecaoCommand { get; }");

text = text.Replace("Action<string>? notificarErro = null)", "Action<string>? notificarErro = null,\n        Action<ColecaoAppViewModel>? onRemoverColecao = null)");

text = text.Replace("EditarColecaoCommand = new RelayCommand(() => { PainelAberto = false; onEditarColecao?.Invoke(this); });", "EditarColecaoCommand = new RelayCommand(() => { PainelAberto = false; onEditarColecao?.Invoke(this); });\n        RemoverColecaoCommand = new RelayCommand(() => { PainelAberto = false; onRemoverColecao?.Invoke(this); });");

File.WriteAllText(path, text);
