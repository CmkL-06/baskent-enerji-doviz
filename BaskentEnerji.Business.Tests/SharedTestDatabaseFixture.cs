using Xunit;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// Test veritabanı şemasını (BaskentEnerjiTests) tüm test sınıfları için TEK SEFER oluşturur.
    /// Her testte ayrı ayrı EnsureCreated() çağırmak yerine, bu koleksiyona üye tüm test sınıfları
    /// bu fixture'ı paylaşır.
    /// </summary>
    public class SharedTestDatabaseFixture
    {
        public SharedTestDatabaseFixture()
        {
            TestDbContextFactory.EnsureCreated();
        }
    }

    [CollectionDefinition("SharedTestDatabase")]
    public class SharedTestDatabaseCollection : ICollectionFixture<SharedTestDatabaseFixture>
    {
    }
}
