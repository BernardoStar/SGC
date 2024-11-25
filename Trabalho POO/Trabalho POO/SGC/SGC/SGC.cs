using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.IO;
using System.Text.Json;
using SGC.SGC.Controller;
using SGC.SGC.Model;
using SGC.SGC.View;
namespace SGC
{
    class Program
    {
        static void Main(string[] args)
        {
            Menu.ExibirMenu();
        }
    }

    namespace SGC.Model
    {
        public abstract class Pessoa
        {
            [JsonInclude]
            public string Nome { get; private set; }
            [JsonInclude]
            public string Email { get; private set; }

            protected Pessoa(string nome, string email)
            {
                if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email))
                    throw new ArgumentException("Nome e email não podem ser vazios.");

                Nome = nome;
                Email = email;
            }

            public abstract void ExibirInformacoes();
        }
        public class Aluno : Pessoa
        {
            public Aluno(string nome, string email) : base(nome, email) { }

            public override void ExibirInformacoes()
            {
                Console.WriteLine($"Aluno: {Nome}, Email: {Email}");
            }
        }

        public class Professor : Pessoa
        {
            public Professor(string nome, string email) : base(nome, email) { }

            public override void ExibirInformacoes()
            {
                Console.WriteLine($"Professor: {Nome}, Email: {Email}");
            }
        }
        public interface ICurso
        {
            string Titulo { get; }
            void AdicionarParticipante(Pessoa participante);
            void ExibirDetalhes();
        }
        public class Curso : ICurso
        {
            [JsonInclude]
            public string Titulo { get; private set; }
            [JsonInclude]
            public List<Pessoa> Participantes { get; private set; }
            [JsonInclude]
            public int CapacidadeMaxima { get; private set; }

            public Curso(string titulo, int capacidadeMaxima)
            {
                if (capacidadeMaxima <= 0) throw new ArgumentException("Capacidade máxima deve ser maior que zero.");

                Titulo = titulo;
                CapacidadeMaxima = capacidadeMaxima;
                Participantes = new List<Pessoa>();
            }

            public virtual void AdicionarParticipante(Pessoa participante)
            {
                if (Participantes.Count >= CapacidadeMaxima)
                    throw new ExcecaoCurso("Capacidade máxima atingida.");

                if (Participantes.Contains(participante))
                    throw new ExcecaoCurso($"Participante {participante.Nome} já está inscrito.");

                Participantes.Add(participante);
            }

            public virtual void ExibirDetalhes()
            {
                Console.WriteLine($"Curso: {Titulo}, Capacidade Máxima: {CapacidadeMaxima}");
                foreach (var p in Participantes)
                    p.ExibirInformacoes();
            }
        }
        public class CursoOnline : Curso
        {
            public string Plataforma { get; }

            public CursoOnline(string titulo, int capacidadeMaxima, string plataforma)
                : base(titulo, capacidadeMaxima)
            {
                Plataforma = plataforma;
            }

            public override void ExibirDetalhes()
            {
                base.ExibirDetalhes();
                Console.WriteLine($"Plataforma: {Plataforma}");
            }
        }

        public class CursoPresencial : Curso
        {
            public string Local { get; }

            public CursoPresencial(string titulo, int capacidadeMaxima, string local)
                : base(titulo, capacidadeMaxima)
            {
                Local = local;
            }

            public override void ExibirDetalhes()
            {
                base.ExibirDetalhes();
                Console.WriteLine($"Local: {Local}");
            }
        }
        public sealed class CursoEspecial : Curso
        {
            public string RequisitosEspeciais { get; }

            public CursoEspecial(string titulo, int capacidadeMaxima, string requisitosEspeciais)
                : base(titulo, capacidadeMaxima)
            {
                RequisitosEspeciais = requisitosEspeciais;
            }

            public override void ExibirDetalhes()
            {
                base.ExibirDetalhes();
                Console.WriteLine($"Requisitos Especiais: {RequisitosEspeciais}");
            }
        }
        public class ExcecaoCurso : Exception
        {
            public ExcecaoCurso(string mensagem) : base(mensagem) { }
        }

        public static class Persistencia
        {
            private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
            };

            public static void SalvarDados<T>(T dados, string caminho)
            {
                try
                {
                    var json = JsonSerializer.Serialize(dados, Options);
                    File.WriteAllText(caminho, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao salvar dados: {ex.Message}");
                }
            }

            public static T CarregarDados<T>(string caminho)
            {
                try
                {
                    if (!File.Exists(caminho)) return default;

                    var json = File.ReadAllText(caminho);
                    return JsonSerializer.Deserialize<T>(json, Options);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao carregar dados: {ex.Message}");
                    return default;
                }
            }
        }
    }
    namespace SGC.Controller
    {
        public class Controller
        {
            private readonly List<Curso> cursos;
            private readonly string arquivoPersistencia = "dados.json";

            public Controller()
            {
                cursos = Persistencia.CarregarDados<List<Curso>>(arquivoPersistencia) ?? new List<Curso>();
            }

            public void AdicionarCurso(Curso curso)
            {
                if (curso == null) throw new ArgumentNullException(nameof(curso));

                if (cursos.Exists(c => c.Titulo == curso.Titulo))
                    throw new ExcecaoCurso($"O curso '{curso.Titulo}' já está registrado.");

                cursos.Add(curso);
                Console.WriteLine($"Curso '{curso.Titulo}' adicionado com sucesso!");
            }

            public void AdicionarParticipante(string tituloCurso, Pessoa participante)
            {
                var curso = cursos.Find(c => c.Titulo == tituloCurso);
                if (curso == null) throw new ExcecaoCurso($"Curso '{tituloCurso}' não encontrado.");

                curso.AdicionarParticipante(participante);
                Console.WriteLine($"{participante.Nome} foi adicionado(a) ao curso '{tituloCurso}'.");
            }

            public void ListarCursos()
            {
                if (cursos.Count == 0)
                {
                    Console.WriteLine("Nenhum curso cadastrado no momento.");
                    return;
                }

                foreach (var curso in cursos)
                {
                    curso.ExibirDetalhes();
                    Console.WriteLine();
                }
            }

            public void SalvarDados()
            {
                Persistencia.SalvarDados(cursos, arquivoPersistencia);
                Console.WriteLine("Dados salvos com sucesso!");
            }
        }
    }
    namespace SGC.View
    {
        public static class Menu
        {
            public static void ExibirMenu()
            {
                var controller = new Controller();
                bool continuar = true;

                while (continuar)
                {
                    Console.Clear();
                    Console.WriteLine("=== Sistema de Gestão de Cursos - SGC ===");
                    Console.WriteLine("1. Cadastrar Curso");
                    Console.WriteLine("2. Cadastrar Aluno ou Professor");
                    Console.WriteLine("3. Listar Cursos");
                    Console.WriteLine("4. Salvar Dados");
                    Console.WriteLine("5. Sair");
                    Console.Write("Escolha uma opção: ");
                    var opcao = Console.ReadLine();

                    try
                    {
                        switch (opcao)
                        {
                            case "1":
                                CadastrarCurso(controller);
                                break;
                            case "2":
                                CadastrarParticipante(controller);
                                break;
                            case "3":
                                controller.ListarCursos();
                                break;
                            case "4":
                                controller.SalvarDados();
                                break;
                            case "5":
                                continuar = false;
                                controller.SalvarDados();
                                Console.WriteLine("Dados salvos e programa encerrado!");
                                break;
                            default:
                                Console.WriteLine("Opção inválida. Tente novamente.");
                                break;
                        }
                    }
                    catch (ExcecaoCurso ex)
                    {
                        Console.WriteLine($"Erro: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro inesperado: {ex.Message}");
                    }

                    Console.WriteLine("\nPressione qualquer tecla para continuar...");
                    Console.ReadKey();
                }
            }

            private static void CadastrarCurso(Controller controller)
            {
                Console.Write("Título do Curso: ");
                var titulo = Console.ReadLine();
                Console.Write("Capacidade Máxima: ");
                int capacidade = int.Parse(Console.ReadLine());

                Console.Write("Tipo de Curso (1 - Online, 2 - Presencial): ");
                var tipo = Console.ReadLine();

                Curso curso = null;
                if (tipo == "1")
                {
                    Console.Write("Plataforma do Curso Online: ");
                    var plataforma = Console.ReadLine();
                    curso = new CursoOnline(titulo, capacidade, plataforma);
                }
                else if (tipo == "2")
                {
                    Console.Write("Local do Curso Presencial: ");
                    var local = Console.ReadLine();
                    curso = new CursoPresencial(titulo, capacidade, local);
                }
                else
                {
                    Console.WriteLine("Tipo de curso inválido.");
                    return;
                }

                controller.AdicionarCurso(curso);
            }

            private static void CadastrarParticipante(Controller controller)
            {
                Console.Write("Título do Curso: ");
                var tituloCurso = Console.ReadLine();

                Console.Write("Tipo de Participante (1 - Aluno, 2 - Professor): ");
                var tipo = Console.ReadLine();

                Console.Write("Nome: ");
                var nome = Console.ReadLine();
                Console.Write("Email: ");
                var email = Console.ReadLine();

                Pessoa participante = null;
                if (tipo == "1")
                    participante = new Aluno(nome, email);
                else if (tipo == "2")
                    participante = new Professor(nome, email);
                else
                {
                    Console.WriteLine("Tipo de participante inválido.");
                    return;
                }

                controller.AdicionarParticipante(tituloCurso, participante);
            }
        }
    }
}

