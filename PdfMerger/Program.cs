using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Diagnostics;
using System.Data;

namespace PdfMerger
{
    class Program
    {

        static void Main(string[] args)
        {

            string origen = args[0];
            string destinoOriginales = args[1];
            string destinoComprimidos = args[2];
            string destinoPaginas = args[3];

            System.Console.WriteLine(" Ingresa 3 rutas, a carpetas como argumentos: 1-origen 2-destino 3-destino de comprimidos . No llevan comillas, los args solo se separan por espacios.");
            string targetFolder = TargetFolder(origen);

            MergeFilesInFolder(targetFolder, destinoOriginales, destinoComprimidos,destinoPaginas);

  
        }

        public static string TargetFolder(string origen)// Busca la carpeta "día" que contiene el diario a hacer merge
        {
            List<byte[]> sourceFiles = new List<byte[]>();

            //origen es carpeta madre donde se anidaran las carpetas con las ediciones "deshojadas". No hace falta que sea la carpeta del mes, puede ser la que contiene los meses

            // Toma una instantanea del directorio  
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(@origen);

            DateTime today = DateTime.Now;

            var año = today.Year;
            string mes;
            string dia;

            if (today.Month <= 9)
            {
                mes = "0" + today.Month;
            }
            else
            {
                mes = today.Month.ToString();
            }

            if (today.Day <= 9)
            {
                dia = "0" + today.Day;
            }
            else
            {
                dia = today.Day.ToString();
            }


            string folderName = "PDF " + dia + "-" + mes + "-" + año;
                      

            IEnumerable<System.IO.FileInfo> fileList = dir.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

            //filtro los archivos pdf de todos los direcctorios y los ordeno por fecha de creación
            IEnumerable<System.IO.FileInfo> fileQuery =
                from file in fileList
                where file.Extension == ".pdf" && file.Directory.Name==folderName
                orderby file.CreationTime
                select file;

            string folderPath;
            if (fileQuery.Any())
            {
                //tomo el archivo más reciente
                var newestFile = fileQuery.Last();
                string nombreCarpeta = newestFile.Directory.Name.Substring(0, 3);
                

                if (nombreCarpeta == "PDF")
                {
                    folderPath = newestFile.Directory.FullName; //Esto es para evitar las carpetas de suplementos
                }
                else
                {
                    folderPath = newestFile.Directory.Parent.FullName;
                }
            }
            else
            {
                folderPath = "";                
            }



            return folderPath;

        }





        public static void MergeFilesInFolder(string folderPath, string destino, string comprimidos, string paginas)// Hace busca los pdf de una carpeta y los  guarda ya unidos en un destino con el nombre ingresado
        {
            if(folderPath!="")
            {
                List<byte[]> sourceFiles = new List<byte[]>();

                // MergeFilesInFolder
                System.IO.DirectoryInfo dir1 = new System.IO.DirectoryInfo(folderPath);

                IEnumerable<System.IO.FileInfo> fileList = dir1.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

                IEnumerable<System.IO.FileInfo> fileQuery =
                    from file in fileList
                    where file.Extension == ".pdf"
                //where file.Directory.Name == "PDF 02-01-2020"
                orderby file.Name
                    select file;

                foreach (System.IO.FileInfo fi in fileQuery)
                {
                    Console.WriteLine(fi.FullName);

                    sourceFiles.Add(System.IO.File.ReadAllBytes(fi.FullName));

                }

                byte[] newPdf = MergeFiles(sourceFiles);

                System.IO.File.WriteAllBytes(@destino + "\\" + dir1.Name + ".pdf", newPdf);//setear destino


                string origenComp = @destino + "//" + dir1.Name + ".pdf";
                string destinoComp = @comprimidos + "//" + dir1.Name + ".pdf";

                CompressPDF(@origenComp, @destinoComp, "screen");


                string destinoCopy = @paginas + "//" + dir1.Name;

        
                System.IO.DirectoryInfo dir2 = new System.IO.DirectoryInfo(@destinoCopy);
                CopyAll(dir1, dir2);
            }

        }



        /// <summary>
        /// Merge pdf files.
        /// </summary>
        /// <param name="sourceFiles">PDF files being merged.</param>
        /// <returns></returns>
        public static byte[] MergeFiles(List<byte[]> sourceFiles) //utiliza la librería externa para unir el string de bytes y crear una solo arreglo de byte,para conformar el nuevo pdf
        {
            Document document = new Document();
            using (MemoryStream ms = new MemoryStream())
            {
                PdfCopy copy = new PdfCopy(document, ms);
                document.Open();
                int documentPageCounter = 0;

                // Iterate through all pdf documents
                for (int fileCounter = 0; fileCounter < sourceFiles.Count; fileCounter++)
                {
                    // Create pdf reader
                    PdfReader reader = new PdfReader(sourceFiles[fileCounter]);
                    int numberOfPages = reader.NumberOfPages;

                    // Iterate through all pages
                    for (int currentPageIndex = 1; currentPageIndex <= numberOfPages; currentPageIndex++)
                    {
                        documentPageCounter++;
                        PdfImportedPage importedPage = copy.GetImportedPage(reader, currentPageIndex);
                        PdfCopy.PageStamp pageStamp = copy.CreatePageStamp(importedPage);

                        // Write header
                        ColumnText.ShowTextAligned(pageStamp.GetOverContent(), Element.ALIGN_CENTER,
                            new Phrase(""), importedPage.Width / 2, importedPage.Height - 30,
                            importedPage.Width < importedPage.Height ? 0 : 1);

                        // Write footer
                        ColumnText.ShowTextAligned(pageStamp.GetOverContent(), Element.ALIGN_CENTER,
                            new Phrase(String.Format("", documentPageCounter)), importedPage.Width / 2, 30,
                            importedPage.Width < importedPage.Height ? 0 : 1);

                        pageStamp.AlterContents();

                        copy.AddPage(importedPage);
                    }

                    copy.FreeReader(reader);
                    reader.Close();
                }

                document.Close();
                return ms.GetBuffer();
            }
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            

            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }



  

        //compression https://www.youtube.com/watch?v=8oc0_w8m640&ab_channel=C%23CodersByH-educate
        //dependencias ghost.exe y gsdll32.dll - ghostscript - bin/debug -bin/release
        private static bool CompressPDF(string InputFile, string OutPutFile, string CompressValue)
        {
            try
            {
                Process proc = new Process();
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.CreateNoWindow = true;
                psi.ErrorDialog = false;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.FileName = "ghsot.exe";


                string args = "-sDEVICE=pdfwrite -dCompatibilityLevel=1.4" + " -dPDFSETTINGS=/" + CompressValue + " -dNOPAUSE  -dQUIET -dBATCH" + " -sOutputFile=\"" + OutPutFile + "\" " + "\"" + InputFile + "\"";


                psi.Arguments = args;


                //start the execution
                proc.StartInfo = psi;

                proc.Start();
                proc.WaitForExit();


                return true;
            }
            catch
            {
                return false;
            }
        }



    }
}
