using System;
using System.ComponentModel.DataAnnotations;

namespace ApiService.Domain.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        public string Id { get; set; } = GenerateUUIDv7();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        private static string GenerateUUIDv7()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var guid = Guid.NewGuid().ToByteArray();

            guid[0] = (byte)((timestamp >> 40) & 0xFF);
            guid[1] = (byte)((timestamp >> 32) & 0xFF);
            guid[2] = (byte)((timestamp >> 24) & 0xFF);
            guid[3] = (byte)((timestamp >> 16) & 0xFF);
            guid[4] = (byte)((timestamp >> 8) & 0xFF);
            guid[5] = (byte)(timestamp & 0xFF);
            guid[6] = (byte)((guid[6] & 0x0F) | 0x70);
            guid[8] = (byte)((guid[8] & 0x3F) | 0x80);

            return new Guid(guid).ToString();
        }
    }
}
