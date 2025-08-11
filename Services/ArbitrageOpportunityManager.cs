namespace Arbitrage.Services;

using System.Collections.Concurrent;
using Arbitrage.SharedModels;
using Microsoft.Extensions.Logging;

/// <summary>
/// Управляет потокобезопасной коллекцией арбитражных возможностей
/// </summary>
public class ArbitrageOpportunityManager
{
    /// <summary>
    /// Потокобезопасная коллекция арбитражных возможностей, индексированная по уникальному ключу
    /// </summary>
    private readonly ConcurrentDictionary<string, ArbitrageOpportunity> _opportunities = new();
    
    public ArbitrageOpportunityManager()
    {
    }
    
    /// <summary>
    /// Добавляет или обновляет арбитражную возможность в коллекции
    /// </summary>
    /// <param name="opportunity">Арбитражная возможность для добавления или обновления</param>
    /// <returns>true если была добавлена новая возможность, false если обновлена существующая</returns>
    public bool AddOrUpdate(ArbitrageOpportunity opportunity)
    {
        if (opportunity == null)
        {
            throw new ArgumentNullException(nameof(opportunity));
        }
        
        string key = opportunity.GetUniqueKey();
        
        // Используем AddOrUpdate для атомарного добавления или обновления
        var isNew = false;
        
        _opportunities.AddOrUpdate(
            key,
            // Функция добавления, если ключа нет
            k => {
                isNew = true;
                return opportunity;
            },
            // Функция обновления, если ключ уже существует
            (k, existingOpportunity) => {
                existingOpportunity.MinPrice = opportunity.MinPrice;
                existingOpportunity.MaxPrice = opportunity.MaxPrice;
                existingOpportunity.PriceDifferencePercent = opportunity.PriceDifferencePercent;
                existingOpportunity.LastUpdatedAt = DateTimeOffset.Now;
                return existingOpportunity;
            });
        
        return isNew;
    }
    
    /// <summary>
    /// Получает все текущие арбитражные возможности
    /// </summary>
    /// <returns>Коллекция арбитражных возможностей</returns>
    public IReadOnlyCollection<ArbitrageOpportunity> GetAll()
    {
        return _opportunities.Values.OrderByDescending(o => o.PriceDifferencePercent).ToList().AsReadOnly();
    }
    
    /// <summary>
    /// Удаляет арбитражную возможность из коллекции
    /// </summary>
    /// <param name="opportunity">Арбитражная возможность для удаления</param>
    /// <returns>true если возможность была удалена, false если такой возможности не было</returns>
    public bool Remove(ArbitrageOpportunity opportunity)
    {
        if (opportunity == null)
        {
            throw new ArgumentNullException(nameof(opportunity));
        }
        
        return _opportunities.TryRemove(opportunity.GetUniqueKey(), out _);
    }
    
    /// <summary>
    /// Получает количество арбитражных возможностей в коллекции
    /// </summary>
    public int Count => _opportunities.Count;
}
