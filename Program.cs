using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace QifAnon {
	class Program {
		static void Main(string[] args) {
			new Program(args).Run();
		}

		string filename;
		string outfile;
		StreamReader qif;
		StreamWriter anon;
		Regex isNumeric = new Regex(@"^[0-9\.,$£\-+]+$");
		Regex notNumeric = new Regex("[^0-9]");
		Regex notAlpha = new Regex("[^a-zA-Z]");
		Regex isDate = new Regex(@"^ ?(\d+)/ ?(\d+)(['/]) ?(\d+)$");
		Dictionary<string, string> numbers = new Dictionary<string, string>();
		Dictionary<string, string> words = new Dictionary<string, string>();
		Dictionary<string, string> dates = new Dictionary<string, string>();
		Random random = new Random();
		const string vowels = "aeiou";
		const string consonants = "bcdfghjklmnpqrstvwxyz";

		Program(string[] args) {
			filename = args[0];
			outfile = Path.Combine(Path.GetDirectoryName(filename), "anon.qif");
		}

		void Run() {
			using(qif = new StreamReader(filename)) {
				using(anon = new StreamWriter(outfile, false)) {
					string line;
					string type = "";
					while((line = qif.ReadLine()) != null) {
						if(line.Length > 0) {
							switch(line[0]) {
								case '!':
									type = line;
									break;
								case '^':
								case '"':
									break;
								case 'N':
									if (type != "!Type:Invst")	// In investments, N is type of transaction ("Buy", "Sell", etc.)
										line = line[0] + Anonymise(line.Substring(1));
									break;
								case 'T':
									if(type != "!Account")	// in Accounts, T is account type, rather than an amount
										line = line[0] + Anonymise(line.Substring(1));
									break;
								default:
									line = line[0] + Anonymise(line.Substring(1));
									break;
							}
						}
						anon.WriteLine(line);
					}
				}
			}
		}

		string Anonymise(string line) {
			Func<char, bool> change;
			string result;
			if(isDate.IsMatch(line)) {
				if (!dates.TryGetValue(line, out result)) {
					Match match = isDate.Match(line);
					int d = int.Parse(match.Groups[1].Value);
					int m = int.Parse(match.Groups[2].Value);
					int y = int.Parse(match.Groups[4].Value);
					if (match.Groups[3].Value == "'")
						y += 2000;
					else if (y < 1000)
						y += 1900;
					d = random.Next(d > 12 ? 28 : 12) + 1;
					m = random.Next(m > 12 ? 28 : 12) + 1;
					y = 1900 + random.Next(118);
					result = string.Format("{0:02}/{1:02}/{2}", d, m, y);
					dates[line] = result;
				}
				return result;
			} else if(isNumeric.IsMatch(line)) {
				string key = notNumeric.Replace(line, "");
				if(!numbers.TryGetValue(key, out result)) {
					char[] r = new char[key.Length];
					for(int i = 0; i < r.Length; i++) {
						if(key[i] == '0') {
							r[i] = (char)('0' + random.Next(10));
						} else {
							r[i] = (char)('1' + random.Next(9));
						}
					}
					result = new string(r);
					numbers[key] = result;
				}
				change = delegate (char c) {
					return c >= '0' && c <= '9';
				};
			} else {
				string key = notAlpha.Replace(line, "").ToLower();
				if(!words.TryGetValue(key, out result)) {
					char[] r = new char[key.Length];
					for (int i = 0; i < r.Length; i++) {
						if(vowels.Contains(key[i])) {
							r[i] = vowels[random.Next(vowels.Length)];
						} else {
							r[i] = consonants[random.Next(consonants.Length)];
						}
					}
					result = new string(r);
					words[key] = result;
				}
				change = delegate (char c) {
					return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
				};
			}
			char[] l = new char[line.Length];
			int next = 0;
			for(int i = 0; i < l.Length; i++) {
				l[i] = change(line[i]) ? result[next++] : line[i];
			}
			return new string(l);
		}
	}
}
