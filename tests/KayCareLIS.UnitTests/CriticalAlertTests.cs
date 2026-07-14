using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KayCareLIS.Core.DTOs.LabOrders;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Interfaces;
using KayCareLIS.Infrastructure.Data;
using KayCareLIS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace KayCareLIS.UnitTests;

public class CriticalAlertTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public CriticalAlertTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ManualResultEntry_ShouldFlagCritical_WhenValueIsOutsideCriticalRange()
    {
        // Arrange
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns(Guid.NewGuid());
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);

        // Setup catalog item with a critical reference range
        var catalogId = Guid.NewGuid();
        var catalogItem = new LabTestCatalog
        {
            LabTestCatalogId = catalogId,
            TestCode = "POTASSIUM",
            TestName = "Potassium",
            Department = "Chemistry",
            IsManualEntry = true,
            DefaultUnit = "mmol/L",
            DefaultReferenceRange = "3.5-5.3",
            CriticalReferenceRange = "2.8-6.0",
            IsActive = true
        };
        db.LabTestCatalog.Add(catalogItem);

        // Setup an order and order item
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var order = new LabOrder
        {
            LabOrderId = orderId,
            TenantId = mockTenantContext.Object.TenantId,
            Status = "Ordered"
        };
        db.LabOrders.Add(order);

        var orderItem = new LabOrderItem
        {
            LabOrderItemId = itemId,
            LabOrderId = orderId,
            LabTestCatalogId = catalogId,
            TestName = "Potassium",
            Department = "Chemistry",
            Status = "SampleCollected",
            TenantId = mockTenantContext.Object.TenantId
        };
        db.LabOrderItems.Add(orderItem);

        await db.SaveChangesAsync();

        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);

        // Act - enter a critical result value (e.g. 1.5, which is < 2.8)
        var req = new ManualResultRequest
        {
            Result = "1.5",
            Unit = "mmol/L",
            ReferenceRange = "3.5-5.3"
        };

        var result = await service.EnterManualResultAsync(itemId, req, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsCritical);

        var dbItem = await db.LabOrderItems.FindAsync(itemId);
        Assert.NotNull(dbItem);
        Assert.True(dbItem.IsCritical);
    }

    [Fact]
    public async Task RecordCriticalCallLogAsync_ShouldCreateLogAndLinkToItem()
    {
        // Arrange
        var mockTenantContext = new Mock<ITenantContext>();
        var tenantId = Guid.NewGuid();
        mockTenantContext.Setup(t => t.TenantId).Returns(tenantId);
        
        var userId = Guid.NewGuid();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(u => u.UserId).Returns(userId);

        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);

        // Add User
        db.Users.Add(new User
        {
            UserId = userId,
            FirstName = "Tech",
            LastName = "One",
            Email = "tech1@kaycare.com",
            TenantId = tenantId
        });

        // Setup catalog
        var catalogId = Guid.NewGuid();
        db.LabTestCatalog.Add(new LabTestCatalog
        {
            LabTestCatalogId = catalogId,
            TestCode = "GLU",
            TestName = "Glucose",
            Department = "Chemistry",
            CriticalReferenceRange = "2.2-25.0"
        });

        // Setup order
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        db.LabOrders.Add(new LabOrder
        {
            LabOrderId = orderId,
            TenantId = mockTenantContext.Object.TenantId,
            Status = "InProgress"
        });

        db.LabOrderItems.Add(new LabOrderItem
        {
            LabOrderItemId = itemId,
            LabOrderId = orderId,
            LabTestCatalogId = catalogId,
            TestName = "Glucose",
            Status = "Resulted",
            IsCritical = true,
            TenantId = mockTenantContext.Object.TenantId
        });

        await db.SaveChangesAsync();

        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);

        var logReq = new CreateCriticalCallLogRequest
        {
            RecipientName = "Dr. Jane Doe",
            Notes = "Confirmed potassium of 1.5, doctor will re-order stat."
        };

        // Act
        var result = await service.RecordCriticalCallLogAsync(itemId, logReq, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CriticalCallLogId);
        Assert.Equal("Dr. Jane Doe", result.CriticalCallLogRecipient);
        Assert.Equal("Confirmed potassium of 1.5, doctor will re-order stat.", result.CriticalCallLogNotes);

        // Verify call log in database
        var dbLog = await db.CriticalCallLogs.FindAsync(result.CriticalCallLogId.Value);
        Assert.NotNull(dbLog);
        Assert.Equal("Dr. Jane Doe", dbLog.RecipientName);
        Assert.Equal("Tech One", dbLog.CalledByName);
        Assert.Equal(itemId, dbLog.LabOrderItemId);

        var dbItem = await db.LabOrderItems.Include(i => i.CriticalCallLog).FirstOrDefaultAsync(i => i.LabOrderItemId == itemId);
        Assert.NotNull(dbItem);
        Assert.NotNull(dbItem.CriticalCallLog);
        Assert.Equal(dbLog.CriticalCallLogId, dbItem.CriticalCallLog.CriticalCallLogId);
    }
}
