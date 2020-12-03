using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace PdfMerger
{
    class Program
    {

        static void Main(string[] args)
        {
            string newestFolder = NewestFolder();

            MergeFilesInFolder(newestFolder);
        }

        public static string NewestFolder()// Busca la carpeta "día" que contiene el diario a hacer merge
        {
            List<byte[]> sourceFiles = new List<byte[]>();

            string startFolder = @"C:\Users\victo\Desktop\abuelo";//Escrbir el path de la carpeta madre donde se anidaran las carpetas con las ediciones "deshojadas". No hace falta que sea la carpeta del mes, puede ser la que contiene los meses

            // Toma una instantanea del directorio  
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(startFolder);

            IEnumerable<System.IO.FileInfo> fileList = dir.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

            //filtro los archivos pdf de todos los direcctorios y los ordeno por fecha de creación
            IEnumerable<System.IO.FileInfo> fileQuery =
                from file in fileList
                where file.Extension == ".pdf"
                orderby file.CreationTime
                select file;

            //tomo el archivo más reciente
            var newestFile = fileQuery.Last();

            //tomo el directorio padre de el archivo más reciente. Es el directorio en el que voy a hacer merge.
            var folderPath = newestFile.Directory.FullName;

            return folderPath;

        }





        public static void MergeFilesInFolder(string folderPath)// Hace busca los pdf de una carpeta y los  guarda ya unidos en un destino con el nombre ingresado
        {
            List<byte[]> sourceFiles = new List<byte[]>();

            // MergeFilesInFolder
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(folderPath);

            IEnumerable<System.IO.FileInfo> fileList = dir.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

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
            System.IO.File.WriteAllBytes(@"C:\Users\victo\Desktop\destino\"+dir.Name+".pdf", newPdf);//setear destino

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
        
    

    }
}
