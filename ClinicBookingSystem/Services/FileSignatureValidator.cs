/*
 FILE          : FileSignatureValidator.cs
 PROJECT       : SECU2000 - Project
 PROGRAMMER    : Nick Turco | 9056530
 FIRST VERSION : 2026-10-12
 DESCRIPTION   : This file contains the FileSignatureValidator class, which checks that the contents of an uploaded file
                 match the content type the client claimed for it. The browser-supplied ContentType is easy to spoof, so
                 binary formats are checked against their file signature (magic bytes), Office Open XML files are opened
                 as ZIP archives and checked for their main part, and text formats are checked for valid UTF-8 with no
                 binary control characters (JSON must also parse). It also checks that the file name's extension is
                 one of the extensions expected for the claimed content type.
*/
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace ClinicBookingSystem.Services
{
    public static class FileSignatureValidator
    {
        private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46, 0x2D };                         // "%PDF-"
        private static readonly byte[] OleSignature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };       // legacy .doc
        private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 };                               // "PK\x03\x04"

        // Strict decoder: throws on invalid UTF-8 instead of silently substituting characters
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        // Extensions accepted for each allowed content type (compared case-insensitively)
        private static readonly Dictionary<string, string[]> AllowedExtensions = new()
        {
            ["image/jpeg"] = new[] { ".jpg", ".jpeg" },
            ["image/png"] = new[] { ".png" },
            ["application/pdf"] = new[] { ".pdf" },
            ["text/plain"] = new[] { ".txt" },
            ["text/csv"] = new[] { ".csv" },
            ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = new[] { ".xlsx" },
            ["application/msword"] = new[] { ".doc" },
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = new[] { ".docx" },
            ["application/json"] = new[] { ".json" }
        };

        // Returns true when the file name ends in an extension expected for the claimed content type,
        // e.g. "photo.png" for image/png. Names without an extension, or with a mismatched one, fail.
        public static bool HasAllowedExtension(string? fileName, string? contentType)
        {
            if (string.IsNullOrWhiteSpace(fileName) || contentType == null) return false;

            // SECURITY: Reject characters that are not valid in a file name. On Windows, "notes.html:.png" has the
            // extension ".png" but saving it writes the data to an alternate data stream of a file named "notes.html".
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;

            return AllowedExtensions.TryGetValue(contentType, out var extensions) &&
                   extensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);
        }

        // Returns true when the file contents are consistent with the claimed content type.
        // Unknown content types always fail.
        public static bool IsValid(byte[] content, string? contentType)
        {
            if (content.Length == 0) return false;

            switch (contentType)
            {
                case "image/jpeg":
                    return StartsWith(content, JpegSignature);

                case "image/png":
                    return StartsWith(content, PngSignature);

                case "application/pdf":
                    return StartsWith(content, PdfSignature);

                case "application/msword":
                    return StartsWith(content, OleSignature);

                // .docx and .xlsx are ZIP archives; a plain ZIP (or any other archive) renamed to .docx is rejected
                // because it is missing the format's main part.
                case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                    return IsOfficeOpenXml(content, "word/document.xml");

                case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                    return IsOfficeOpenXml(content, "xl/workbook.xml");

                // Text formats have no signature, so check that the bytes really are text
                case "text/plain":
                case "text/csv":
                    return TryDecodeText(content, out _);

                case "application/json":
                    return TryDecodeText(content, out var json) && IsValidJson(json);

                default:
                    return false;
            }
        }

        private static bool StartsWith(byte[] content, byte[] signature)
        {
            return content.AsSpan().StartsWith(signature);
        }

        // Opens the archive and checks that the expected parts exist. Only the central directory is read; no entry is
        // decompressed, so a ZIP bomb cannot expand here.
        private static bool IsOfficeOpenXml(byte[] content, string mainPart)
        {
            if (!StartsWith(content, ZipSignature)) return false;

            try
            {
                using var archive = new ZipArchive(new MemoryStream(content, writable: false), ZipArchiveMode.Read);
                return archive.GetEntry("[Content_Types].xml") != null && archive.GetEntry(mainPart) != null;
            }
            catch (InvalidDataException)
            {
                // Corrupt or truncated archive
                return false;
            }
        }

        // Accepts UTF-8 (with or without a BOM) containing no control characters other than tab, CR and LF.
        // Rejects binary files (executables, images, archives) uploaded with a text content type.
        private static bool TryDecodeText(byte[] content, out string text)
        {
            text = string.Empty;

            try
            {
                text = StrictUtf8.GetString(content);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }

            // Skip a leading byte order mark
            var start = text.Length > 0 && text[0] == '﻿' ? 1 : 0;

            for (var i = start; i < text.Length; i++)
            {
                var c = text[i];
                if (char.IsControl(c) && c != '\t' && c != '\r' && c != '\n')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidJson(string text)
        {
            try
            {
                using var document = JsonDocument.Parse(text.TrimStart('﻿'));
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
