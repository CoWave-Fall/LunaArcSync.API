using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using LunaArcSync.Api.Core.Interfaces;
using LunaArcSync.Api.Infrastructure.Data; // 需要 AppDbContext
using System;
using System.IO;
using System.Threading.Tasks;
using Tesseract;
using LunaArcSync.Api.DTOs.Ocr; // 引入新的 DTOs
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // 引入 System.Text.Json

namespace LunaArcSync.Api.Infrastructure.Services
{
    public class TesseractOcrService : IOcrService
    {
        private readonly ILogger<TesseractOcrService> _logger;
        private readonly AppDbContext _context;
        private readonly string _storageRootPath;

        public TesseractOcrService(ILogger<TesseractOcrService> logger, AppDbContext context, IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            // 我们需要知道文件存储在哪里才能读取它们
            _storageRootPath = Path.Combine(environment.ContentRootPath, "FileStorage");
        }

        public async Task<string> PerformOcrAsync(Guid versionId)
        {
            var version = await _context.Versions.FindAsync(versionId);
            if (version == null)
            {
                _logger.LogWarning("OCR requested for non-existent version ID: {VersionId}", versionId);
                throw new FileNotFoundException("Version not found.");
            }

            var imagePath = Path.Combine(_storageRootPath, version.ImagePath);
            if (!File.Exists(imagePath))
            {
                _logger.LogError("Image file not found for version ID {VersionId} at path: {ImagePath}", versionId, imagePath);
                throw new FileNotFoundException("Image file not found.", imagePath);
            }

            try
            {
                _logger.LogInformation("Starting detailed OCR for image: {ImagePath}", imagePath);
                using (var engine = new TesseractEngine("./tessdata", "chi_sim+eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(imagePath))
                    {
                        // 使用 page.GetIterator() 来获取详细的、带坐标的结果
                        using (var page = engine.Process(img))
                        {
                            var ocrResult = new OcrResultDto
                            {
                                ImageWidth = img.Width,
                                ImageHeight = img.Height
                            };
                            var allWords = new List<string>();

                            using (var iter = page.GetIterator())
                            {
                                iter.Begin();

                                do // 遍历每一行
                                {
                                    var line = new TextLineDto();
                                    if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var lineBox))
                                    {
                                        line.Bbox = new BoundingBoxDto { X1 = lineBox.X1, Y1 = lineBox.Y1, X2 = lineBox.X2, Y2 = lineBox.Y2 };
                                    }

                                    do // 遍历行中的每一个词
                                    {
                                        if (iter.IsAtBeginningOf(PageIteratorLevel.Word))
                                        {
                                            var wordText = iter.GetText(PageIteratorLevel.Word);
                                            if (!string.IsNullOrWhiteSpace(wordText))
                                            {
                                                var word = new WordDto
                                                {
                                                    Text = wordText.Trim(),
                                                    Confidence = iter.GetConfidence(PageIteratorLevel.Word)
                                                };

                                                if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var wordBox))
                                                {
                                                    word.Bbox = new BoundingBoxDto { X1 = wordBox.X1, Y1 = wordBox.Y1, X2 = wordBox.X2, Y2 = wordBox.Y2 };
                                                }
                                                line.Words.Add(word);
                                                allWords.Add(word.Text);
                                            }
                                        }
                                    } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));

                                    if (line.Words.Count > 0)
                                    {
                                        ocrResult.Lines.Add(line);
                                    }

                                } while (iter.Next(PageIteratorLevel.TextLine));
                            }

                            // 1. 将结构化结果序列化为 JSON 字符串并保存
                            var jsonResult = JsonSerializer.Serialize(ocrResult);
                            version.OcrData = jsonResult;

                            // 2. 创建规范化文本用于搜索
                            var normalizedText = new string(string.Join("", allWords).Where(c => !char.IsWhiteSpace(c)).ToArray());
                            version.OcrDataNormalized = normalizedText;

                            _context.Versions.Update(version);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation("Detailed OCR completed for image: {ImagePath}. Found {LineCount} lines.", imagePath, ocrResult.Lines.Count);

                            return jsonResult; // 返回 JSON 字符串
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred during detailed Tesseract OCR processing for image {ImagePath}", imagePath);
                throw;
            }
        }
    }
}