using System;
using System.Collections.Generic;
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

public class LabTestCatalogTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public LabTestCatalogTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CreateTestCatalogItemAsync_ShouldAddItemAndSetDefaults()
    {
        // Arrange
        var mockTenantContext = new Mock<ITenantContext>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);
        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);

        var request = new CreateLabTestCatalogRequest
        {
            TestCode = "NEWTEST",
            TestName = "New Clinical Test",
            Department = "Microbiology",
            IsManualEntry = true,
            TatHours = 24,
            DefaultUnit = "CFU/mL",
            DefaultReferenceRange = "<10000"
        };

        // Act
        var result = await service.CreateTestCatalogItemAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NEWTEST", result.TestCode);
        Assert.Equal("New Clinical Test", result.TestName);
        Assert.Equal("Microbiology", result.Department);
        Assert.True(result.IsManualEntry);
        Assert.Equal(24, result.TatHours);
        Assert.Equal("CFU/mL", result.DefaultUnit);
        Assert.Equal("<10000", result.DefaultReferenceRange);

        // Verify in DB
        var dbItem = await db.LabTestCatalog.FirstOrDefaultAsync(t => t.TestCode == "NEWTEST");
        Assert.NotNull(dbItem);
        Assert.True(dbItem.IsActive);
    }

    [Fact]
    public async Task CreateTestCatalogItemAsync_ShouldThrowException_WhenTestCodeExists()
    {
        // Arrange
        var mockTenantContext = new Mock<ITenantContext>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);
        db.LabTestCatalog.Add(new LabTestCatalog
        {
            LabTestCatalogId = Guid.NewGuid(),
            TestCode = "DUPE",
            TestName = "Original Test",
            Department = "Haematology"
        });
        await db.SaveChangesAsync();

        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);
        var request = new CreateLabTestCatalogRequest
        {
            TestCode = "DUPE",
            TestName = "Another Test",
            Department = "Chemistry"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateTestCatalogItemAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTestCatalogItemAsync_ShouldModifyExistingFields()
    {
        // Arrange
        var mockTenantContext = new Mock<ITenantContext>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        var testId = Guid.NewGuid();
        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);
        db.LabTestCatalog.Add(new LabTestCatalog
        {
            LabTestCatalogId = testId,
            TestCode = "OLDCODE",
            TestName = "Old Name",
            Department = "Haematology",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);
        var request = new UpdateLabTestCatalogRequest
        {
            TestCode = "UPDATEDCODE",
            TestName = "Updated Name",
            Department = "Chemistry",
            IsManualEntry = false,
            TatHours = 6,
            DefaultUnit = "mmol/L",
            DefaultReferenceRange = "3.5-5.5",
            IsActive = false
        };

        // Act
        var result = await service.UpdateTestCatalogItemAsync(testId, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UPDATEDCODE", result.TestCode);
        Assert.Equal("Updated Name", result.TestName);

        var dbItem = await db.LabTestCatalog.FindAsync(testId);
        Assert.NotNull(dbItem);
        Assert.Equal("UPDATEDCODE", dbItem.TestCode);
        Assert.Equal("Updated Name", dbItem.TestName);
        Assert.Equal("Chemistry", dbItem.Department);
        Assert.False(dbItem.IsActive);
    }

    [Fact]
    public async Task DeleteTestCatalogItemAsync_ShouldHardDelete_WhenNotReferenced()
    {
        // Arrange
        var mockTenantContext = new Mock<ITenantContext>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        var testId = Guid.NewGuid();
        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);
        db.LabTestCatalog.Add(new LabTestCatalog
        {
            LabTestCatalogId = testId,
            TestCode = "DELETEABLE",
            TestName = "Deleteable Test",
            Department = "Haematology"
        });
        await db.SaveChangesAsync();

        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);

        // Act
        await service.DeleteTestCatalogItemAsync(testId, CancellationToken.None);

        // Assert
        var exists = await db.LabTestCatalog.AnyAsync(t => t.LabTestCatalogId == testId);
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteTestCatalogItemAsync_ShouldSoftDelete_WhenReferenced()
    {
        // Arrange
        var mockTenantContext = new Mock<ITenantContext>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();

        var testId = Guid.NewGuid();
        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);
        db.LabTestCatalog.Add(new LabTestCatalog
        {
            LabTestCatalogId = testId,
            TestCode = "REFERENCED",
            TestName = "Referenced Test",
            Department = "Haematology",
            IsActive = true
        });

        // Add a mock lab order item referencing this catalog item
        db.LabOrderItems.Add(new LabOrderItem
        {
            LabOrderItemId = Guid.NewGuid(),
            LabOrderId = Guid.NewGuid(),
            LabTestCatalogId = testId,
            TestName = "Referenced Test",
            Department = "Haematology"
        });

        await db.SaveChangesAsync();

        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);

        // Act
        await service.DeleteTestCatalogItemAsync(testId, CancellationToken.None);

        // Assert
        var dbItem = await db.LabTestCatalog.FindAsync(testId);
        Assert.NotNull(dbItem);
        Assert.False(dbItem.IsActive); // Soft-deleted/Deactivated
    }
}
