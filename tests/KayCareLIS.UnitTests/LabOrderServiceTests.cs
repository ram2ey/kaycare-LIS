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
using KayCareLIS.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace KayCareLIS.UnitTests;

public class LabOrderServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public LabOrderServiceTests()
    {
        // Use unique database name per test run
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Theory]
    [InlineData(null, "3.5-5.0", null)]
    [InlineData("4.0", null, null)]
    [InlineData("not-numeric", "3.5-5.0", null)]
    [InlineData("4.0", "invalid-range", null)]
    [InlineData("3.4", "3.5-5.0", "L")]
    [InlineData("5.1", "3.5-5.0", "H")]
    [InlineData("4.0", "3.5-5.0", "N")]
    [InlineData("3.5", "3.5-5.0", "N")]
    [InlineData("5.0", "3.5-5.0", "N")]
    public void ComputeFlag_ShouldEvaluateCorrectly(string value, string range, string expectedFlag)
    {
        // Act
        var result = LabOrderService.ComputeFlag(value, range);

        // Assert
        Assert.Equal(expectedFlag, result);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldGenerateUniqueSequentialAccessionNumbers_ForMultipleTests()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns(tenantId);

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(u => u.UserId).Returns(doctorId);

        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);

        // Seed Roles, Patient, Doctor, and LabTestCatalog
        var adminRole = new Role { RoleId = 2, RoleName = "Admin" };
        var doctorRole = new Role { RoleId = 3, RoleName = "Doctor" };
        db.Roles.AddRange(adminRole, doctorRole);

        var doctor = new User
        {
            UserId = doctorId,
            TenantId = tenantId,
            RoleId = doctorRole.RoleId,
            Email = "doctor@demo.com",
            PasswordHash = "dummyhash",
            FirstName = "Ordering",
            LastName = "Doctor",
            IsActive = true
        };
        db.Users.Add(doctor);

        var patient = new Patient
        {
            PatientId = patientId,
            TenantId = tenantId,
            MedicalRecordNumber = "MRN-12345",
            FirstName = "Jane",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "Female",
            IsActive = true,
            RegisteredByUserId = doctorId
        };
        db.Patients.Add(patient);

        var test1 = new LabTestCatalog
        {
            LabTestCatalogId = Guid.NewGuid(),
            TestCode = "FBC",
            TestName = "Full Blood Count",
            Department = "Haematology",
            IsActive = true
        };
        var test2 = new LabTestCatalog
        {
            LabTestCatalogId = Guid.NewGuid(),
            TestCode = "ESR",
            TestName = "Erythrocyte Sedimentation Rate",
            Department = "Haematology",
            IsActive = true
        };
        db.LabTestCatalog.AddRange(test1, test2);

        await db.SaveChangesAsync();

        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);

        var request = new CreateLabOrderRequest
        {
            PatientId = patientId,
            TestIds = new List<Guid> { test1.LabTestCatalogId, test2.LabTestCatalogId },
            Notes = "Testing accession numbers"
        };

        // Act
        var result = await service.PlaceOrderAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);

        var item1 = result.Items.First();
        var item2 = result.Items.Last();

        Assert.NotNull(item1.AccessionNumber);
        Assert.NotNull(item2.AccessionNumber);
        Assert.NotEqual(item1.AccessionNumber, item2.AccessionNumber);

        var year = DateTime.UtcNow.Year;
        Assert.StartsWith($"ACC-{year}-", item1.AccessionNumber);
        Assert.StartsWith($"ACC-{year}-", item2.AccessionNumber);

        // Parse numerical part to verify sequential ordering (e.g. ...00001 and ...00002)
        var seq1 = int.Parse(item1.AccessionNumber.Split('-').Last());
        var seq2 = int.Parse(item2.AccessionNumber.Split('-').Last());

        Assert.Equal(1, Math.Abs(seq2 - seq1));
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldThrowAppException_WhenLabModuleIsDisabled()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns(tenantId);

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(u => u.UserId).Returns(doctorId);
        mockCurrentUserService.Setup(u => u.Role).Returns("Doctor");

        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);

        // Seed disabled laboratory module
        db.FacilitySettings.Add(new FacilitySettings
        {
            FacilitySettingsId = Guid.NewGuid(),
            FacilityName = "Test",
            IsLaboratoryEnabled = false,
            IsRadiologyEnabled = true
        });
        await db.SaveChangesAsync();

        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);
        var request = new CreateLabOrderRequest { PatientId = Guid.NewGuid(), TestIds = new List<Guid> { Guid.NewGuid() } };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => service.PlaceOrderAsync(request, CancellationToken.None));
        Assert.Equal("Laboratory module is disabled.", ex.Message);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldThrowForbiddenException_WhenUserIsRadiologyStaff()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns(tenantId);

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(u => u.UserId).Returns(doctorId);
        mockCurrentUserService.Setup(u => u.Role).Returns("Doctor");
        mockCurrentUserService.Setup(u => u.Department).Returns("Radiology");

        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);

        // Seed enabled laboratory module
        db.FacilitySettings.Add(new FacilitySettings
        {
            FacilitySettingsId = Guid.NewGuid(),
            FacilityName = "Test",
            IsLaboratoryEnabled = true,
            IsRadiologyEnabled = true
        });
        await db.SaveChangesAsync();

        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);
        var request = new CreateLabOrderRequest { PatientId = Guid.NewGuid(), TestIds = new List<Guid> { Guid.NewGuid() } };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.PlaceOrderAsync(request, CancellationToken.None));
        Assert.Equal("Radiology staff cannot access Laboratory data.", ex.Message);
    }

    [Fact]
    public async Task PlaceOrderAsync_ShouldNotThrowForbiddenException_WhenUserIsAdminWithRadiologyDepartment()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns(tenantId);

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(u => u.UserId).Returns(doctorId);
        mockCurrentUserService.Setup(u => u.Role).Returns("Admin");
        mockCurrentUserService.Setup(u => u.Department).Returns("Radiology");

        using var db = new AppDbContext(_dbOptions, mockTenantContext.Object);

        // Seed enabled laboratory module
        db.FacilitySettings.Add(new FacilitySettings
        {
            FacilitySettingsId = Guid.NewGuid(),
            FacilityName = "Test",
            IsLaboratoryEnabled = true,
            IsRadiologyEnabled = true
        });
        
        var doctorRole = new Role { RoleId = 3, RoleName = "Doctor" };
        db.Roles.Add(doctorRole);
        db.Users.Add(new User
        {
            UserId = doctorId,
            TenantId = tenantId,
            RoleId = doctorRole.RoleId,
            Email = "admin@demo.com",
            PasswordHash = "dummy",
            FirstName = "Admin",
            LastName = "User",
            IsActive = true
        });
        db.Patients.Add(new Patient
        {
            PatientId = patientId,
            TenantId = tenantId,
            MedicalRecordNumber = "MRN-1",
            FirstName = "Jane",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "Female",
            IsActive = true,
            RegisteredByUserId = doctorId
        });
        var test = new LabTestCatalog
        {
            LabTestCatalogId = Guid.NewGuid(),
            TestCode = "FBC",
            TestName = "Full Blood Count",
            Department = "Haematology",
            IsActive = true
        };
        db.LabTestCatalog.Add(test);
        await db.SaveChangesAsync();

        var service = new LabOrderService(db, mockTenantContext.Object, mockCurrentUserService.Object);
        var request = new CreateLabOrderRequest { PatientId = patientId, TestIds = new List<Guid> { test.LabTestCatalogId } };

        // Act & Assert
        var result = await service.PlaceOrderAsync(request, CancellationToken.None);
        Assert.NotNull(result);
    }
}
