using AutoMapper;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Db;
using RestaurantPOS.Hubs;
using RestaurantPOS.Models;
using RestaurantPOS.Models.Dtos;
using RestaurantPOS.Models.Requests;
using RestaurantPOS.Models.Restaurant;
using RestaurantPOS.Models.Response;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace RestaurantPOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
   // [Authorize(Roles = "Admin")]
    [EnableCors("CorsPolicy")]
    public class AdminController : ControllerBase
    {

        private readonly DbConfig _dbConfig;
        private readonly ILogger<AdminController> _logger;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<OrderHub> _hubContext;

        public AdminController(ILogger<AdminController> logger, DbConfig dbConfig, IMapper mapper, IConfiguration configuration, IHubContext<OrderHub> hubContext)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _mapper = mapper;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        // Helper method to get Commercial User ID
        private int GetCommercialUserId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
            
            if (user != null && user.Role == "Commercial")
            {
                return userId;
            }
            
            return user?.InsertByUserId ?? userId;
        }

        /// <summary>رمز دخول رقمي 4–12 خانة؛ فارغ = لا يُستخدم</summary>
        private static string? NormalizeLoginCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            if (s.Length < 4 || s.Length > 12 || !s.All(char.IsDigit)) return null;
            return s;
        }

        // Add User
        [Authorize(Roles = "Commercial,Admin")]
        [HttpPost("AddUser")]
        public async Task<ActionResult<GlobalResponse<User>>> AddUser([FromForm] UserRequest request)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var currentUser = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);
                
                // Only Admin can add Commercial users
                if (request.Role == "Commercial" && currentUser?.Role != "Admin")
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "ليس لديك صلاحية لإضافة مستخدمين تجاريين. فقط المدير الرئيسي يمكنه ذلك"
                    });
                }

                var commercialUserId = GetCommercialUserId();
                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber && x.IsDeleted == false);
                
                // Check if phone number exists for the same commercial user or globally if Admin
                if (currentUser?.Role == "Admin")
                {
                    // Admin can check globally
                    if (user != null)
                    {
                        return BadRequest(new GlobalResponse<User>
                        {
                            Data = user,
                            ErrorStatus = true,
                            Message = "رقم الهاتف موجود بالفعل"
                        });
                    }
                }
                else
                {
                    // Commercial users can only check within their own users
                    if (user != null && user.InsertByUserId == commercialUserId)
                    {
                        return BadRequest(new GlobalResponse<User>
                        {
                            Data = user,
                            ErrorStatus = true,
                            Message = "رقم الهاتف موجود بالفعل"
                        });
                    }
                }

                // Validate password is provided for new users
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "كلمة المرور مطلوبة لإضافة مستخدم جديد"
                    });
                }

                var newUse = _mapper.Map<User>(request);
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                newUse.Password = passwordHash;
                
                // Set InsertByUserId based on role
                if (request.Role == "Commercial" && currentUser?.Role == "Admin")
                {
                    // Admin creating Commercial user - set InsertByUserId to Admin's ID or 0
                    newUse.InsertByUserId = currentUserId;
                }
                else
                {
                    // Commercial user creating sub-user
                    newUse.InsertByUserId = commercialUserId;
                }
                
                // Handle logo upload for Commercial users created by Admin
                if (request.Role == "Commercial" && currentUser?.Role == "Admin" && request.Logo != null && request.Logo.Length > 0)
                {
                    try
                    {
                        newUse.Logo = await UploadIamgesAsync(request.Logo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading logo for new Commercial user");
                        return BadRequest(new GlobalResponse<User>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = $"خطأ في رفع الشعار: {ex.Message}"
                        });
                    }
                }
                
                // Set restaurant name for Commercial users created by Admin
                if (request.Role == "Commercial" && currentUser?.Role == "Admin" && !string.IsNullOrEmpty(request.RestaurantName))
                {
                    newUse.RestaurantName = request.RestaurantName;
                }

                if (request.Role == "Commercial" && currentUser?.Role == "Admin" && !string.IsNullOrWhiteSpace(request.LoginCode))
                {
                    var lc = NormalizeLoginCode(request.LoginCode);
                    if (lc == null)
                    {
                        return BadRequest(new GlobalResponse<User>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "رمز الدخول يجب أن يكون من 4 إلى 12 رقماً"
                        });
                    }
                    if (await _dbConfig.Users.AnyAsync(u => u.LoginCode == lc && !u.IsDeleted))
                    {
                        return BadRequest(new GlobalResponse<User>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "رمز الدخول مستخدم من حساب آخر"
                        });
                    }
                    newUse.LoginCode = lc;
                }
                
                _dbConfig.Users.Add(newUse);
                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<User>
                {
                    Data = newUse,
                    ErrorStatus = false,
                    Message = "تم إضافة المستخدم بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user");
                return StatusCode(500, new GlobalResponse<User>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء إضافة المستخدم: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpPut("UpdateUser")]
        public async Task<ActionResult<GlobalResponse<User>>> UpdateUser([FromForm] UserRequest request, int id)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var currentUser = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);
                
                // Check if user exists
                var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
                if (user == null)
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المستخدم غير موجود"
                    });
                }

                // Only Admin can update Commercial users
                if (user.Role == "Commercial" && currentUser?.Role != "Admin")
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "ليس لديك صلاحية لتعديل المستخدمين التجاريين. فقط المدير الرئيسي يمكنه ذلك"
                    });
                }

                // Commercial users can only update their own sub-users (not Commercial)
                var commercialUserId = GetCommercialUserId();
                if (currentUser?.Role != "Admin" && user.Role != "Commercial" && user.InsertByUserId != commercialUserId)
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "ليس لديك صلاحية لتعديل هذا المستخدم"
                    });
                }

                // Prevent Commercial users from changing role to Commercial
                if (currentUser?.Role != "Admin" && request.Role == "Commercial")
                {
                    return BadRequest(new GlobalResponse<User>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "ليس لديك صلاحية لتغيير الدور إلى تجاري. فقط المدير الرئيسي يمكنه ذلك"
                    });
                }

                // Store old values for audit log
                var oldValues = new
                {
                    Name = user.Name,
                    PhoneNumber = user.PhoneNumber,
                    Username = user.Username,
                    Role = user.Role,
                    RestaurantName = user.RestaurantName,
                    Logo = user.Logo,
                    LoginCode = user.LoginCode
                };

                // Update basic fields
                user.Name = request.Name;
                user.PhoneNumber = request.PhoneNumber;
                user.Username = request.Username;
                user.Role = request.Role;
                
                // Update password only if provided and not empty
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }

                // Admin can update Logo and RestaurantName for Commercial users
                if (currentUser?.Role == "Admin" && user.Role == "Commercial")
                {
                    // Upload logo if provided and has content
                    if (request.Logo != null && request.Logo.Length > 0)
                    {
                        try
                        {
                            user.Logo = await UploadIamgesAsync(request.Logo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading logo for user {UserId}", id);
                            return BadRequest(new GlobalResponse<User>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"خطأ في رفع الشعار: {ex.Message}"
                            });
                        }
                    }
                    
                    // Update restaurant name if provided
                    if (!string.IsNullOrWhiteSpace(request.RestaurantName))
                    {
                        user.RestaurantName = request.RestaurantName;
                    }

                    if (string.IsNullOrWhiteSpace(request.LoginCode))
                    {
                        user.LoginCode = null;
                    }
                    else
                    {
                        var lc = NormalizeLoginCode(request.LoginCode);
                        if (lc == null)
                        {
                            return BadRequest(new GlobalResponse<User>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = "رمز الدخول يجب أن يكون من 4 إلى 12 رقماً"
                            });
                        }
                        if (await _dbConfig.Users.AnyAsync(u => u.LoginCode == lc && u.Id != id && !u.IsDeleted))
                        {
                            return BadRequest(new GlobalResponse<User>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = "رمز الدخول مستخدم من حساب آخر"
                            });
                        }
                        user.LoginCode = lc;
                    }
                }

                // Store new values for audit log
                var newValues = new
                {
                    Name = user.Name,
                    PhoneNumber = user.PhoneNumber,
                    Username = user.Username,
                    Role = user.Role,
                    RestaurantName = user.RestaurantName,
                    Logo = user.Logo,
                    LoginCode = user.LoginCode
                };

                _dbConfig.Users.Update(user);
                await _dbConfig.SaveChangesAsync();

                // Log audit
                await _dbConfig.LogAuditAsync(
                    "Update",
                    "User",
                    user.Id,
                    user.Name,
                    currentUserId,
                    commercialUserId,
                    oldValues,
                    newValues,
                    $"تم تعديل المستخدم: {user.Name}"
                );

                return Ok(new GlobalResponse<User>
                {
                    Data = user,
                    ErrorStatus = false,
                    Message = "تم تحديث المستخدم بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new GlobalResponse<User>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء تحديث المستخدم: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpDelete("DeleteUser")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteUser(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var currentUser = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == userId);
            var commercialUserId = GetCommercialUserId();
            var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && x.InsertByUserId == commercialUserId);
            if (user == null)
            {
                return BadRequest(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = true,
                    Message = "user not exsit"
                });
            }

            var userName = user.Name;
            user!.IsDeleted = true;
            _dbConfig.Users.Update(user);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            await _dbConfig.LogAuditAsync(
                "Delete",
                "User",
                user.Id,
                userName,
                userId,
                commercialUserId,
                null,
                null,
                $"تم حذف المستخدم: {userName}"
            );

            return Ok(new GlobalResponse<int>
            {
                Data = id,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetUsers")]
        public ActionResult<GlobalResponse<PagedList<User>>> GetUsers(int pageNumber, int pageSize, string? info)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var userInfo = _dbConfig.Users.FirstOrDefault(x => x.Id == userId && x.IsDeleted == false);

            if (userInfo != null && userInfo.Role == "Admin")
            {
                var user = _dbConfig.Users.Where(x => x.IsDeleted == false).AsQueryable();

                if (info != null)
                {
                    user = user.Where(x => x.PhoneNumber == info || x.Name.Contains(info) || x.Username.Contains(info));
                }
                var totalItems = user.Count();

                var pagedResult = new PagedList<User>(user.ToList(), totalItems, pageNumber, pageSize);

                var response = new GlobalResponse<PagedList<User>>
                {
                    Data = pagedResult,
                    ErrorStatus = false,
                    Message = "Success"
                };

                return response;
            }
            else
            {
                var commercialUserId = GetCommercialUserId();
                var user = _dbConfig.Users.Where(x => x.IsDeleted == false && x.InsertByUserId == commercialUserId).AsQueryable();

                if (info != null)
                {
                    user = user.Where(x => x.PhoneNumber == info || x.Name.Contains(info) || x.Username.Contains(info));
                }
                var totalItems = user.Count();



                var pagedResult = new PagedList<User>(user.ToList(), totalItems, pageNumber, pageSize);

                var response = new GlobalResponse<PagedList<User>>
                {
                    Data = pagedResult,
                    ErrorStatus = false,
                    Message = "Success"
                };

                return response;
            }   


        
        }


        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("AddTag")]
        public async Task<ActionResult<GlobalResponse<Tag>>> AddTag(TagRequset request)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _dbConfig.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            var userInsertByUserId = user?.InsertByUserId ?? userId;

            if (request.ParentTagId.HasValue)
            {
                var parent = await _dbConfig.Tags
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(x => x.Id == request.ParentTagId.Value && x.IsDeleted == false);
                if (parent == null)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "القسم الأب غير موجود"
                    });
                }

                var parentScoped = parent.InsertByUserId == userId ||
                    (parent.User != null && (parent.User.Id == userInsertByUserId || parent.User.InsertByUserId == userId));
                if (!parentScoped)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "القسم الأب غير موجود"
                    });
                }

                if (parent.ParentTagId != null)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يُسمح بمستوين فقط: قسم رئيسي ثم قسم فرعي"
                    });
                }
            }

            var tag = await _dbConfig.Tags.FirstOrDefaultAsync(x =>
                x.Name == request.Name && x.IsDeleted == false && x.InsertByUserId == userId &&
                x.ParentTagId == request.ParentTagId);
            if (tag != null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = tag,
                    ErrorStatus = true,
                    Message = "Tag is already exsit"
                });
            }
            var newTag = _mapper.Map<Tag>(request);
            newTag.InsertByUserId = userId;
            _dbConfig.Tags.Add(newTag);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Tag>
            {
                Data = newTag,
                ErrorStatus = false,
                Message = "done"
            });
        }


        // GET: api/Admin/CommercialUserInfo
        [Authorize(Roles = "Commercial,POS,Admin")]
        [HttpGet("CommercialUserInfo")]
        public async Task<ActionResult<GlobalResponse<CommercialUserInfoDto>>> GetCommercialUserInfo()
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                var commercialUser = await _dbConfig.Users
                    .FirstOrDefaultAsync(u => u.Id == commercialUserId && !u.IsDeleted);

                if (commercialUser == null)
                {
                    return NotFound(new GlobalResponse<CommercialUserInfoDto>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "المستخدم غير موجود"
                    });
                }

                var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";

                var userInfo = new CommercialUserInfoDto
                {
                    RestaurantName = commercialUser.RestaurantName ?? commercialUser.Name,
                    Logo = string.IsNullOrEmpty(commercialUser.Logo) ? null : imageBaseUrl + commercialUser.Logo
                };

                return Ok(new GlobalResponse<CommercialUserInfoDto>
                {
                    Data = userInfo,
                    ErrorStatus = false,
                    Message = "تم جلب المعلومات بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commercial user info");
                return StatusCode(500, new GlobalResponse<CommercialUserInfoDto>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء جلب المعلومات: {ex.Message}"
                });
            }
        }

        // updata tag
        // Update User 
        [Authorize(Roles = "Commercial")]

        [HttpPut("UpdateTag")]
        public async Task<ActionResult<GlobalResponse<Tag>>> UpdateTag(TagRequset request, int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var tag = await _dbConfig.Tags
                .Include(t => t.User)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User!.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
            if (tag == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = tag,
                    ErrorStatus = true,
                    Message = "tag not exsit"
                });
            }

            if (request.ParentTagId == id)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "لا يمكن أن يكون القسم أبًا لنفسه"
                });
            }

            var hasChildren = await _dbConfig.Tags.AnyAsync(x => x.ParentTagId == id && x.IsDeleted == false);
            if (hasChildren && request.ParentTagId != null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "لا يمكن ربط قسم له أقسام فرعية كقسم فرعي تحت قسم آخر"
                });
            }

            if (request.ParentTagId.HasValue)
            {
                var parent = await _dbConfig.Tags
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(x => x.Id == request.ParentTagId.Value && x.IsDeleted == false);
                if (parent == null)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "القسم الأب غير موجود"
                    });
                }

                var parentScoped = parent.InsertByUserId == userId ||
                    (parent.User != null && (parent.User.Id == user.InsertByUserId || parent.User.InsertByUserId == userId));
                if (!parentScoped)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "القسم الأب غير موجود"
                    });
                }

                if (parent.ParentTagId != null)
                {
                    return BadRequest(new GlobalResponse<Tag>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يُسمح بمستوين فقط: قسم رئيسي ثم قسم فرعي"
                    });
                }
            }

            var duplicate = await _dbConfig.Tags.FirstOrDefaultAsync(x =>
                x.Id != id &&
                x.Name == request.Name && x.IsDeleted == false && x.InsertByUserId == userId &&
                x.ParentTagId == request.ParentTagId);
            if (duplicate != null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = duplicate,
                    ErrorStatus = true,
                    Message = "Tag is already exsit"
                });
            }

            // Store old values for audit log
            var oldValues = new
            {
                Name = tag.Name,
                ParentTagId = tag.ParentTagId
            };

            var uTag = _mapper.Map(request, tag);

            // Store new values for audit log
            var newValues = new
            {
                Name = uTag.Name,
                ParentTagId = uTag.ParentTagId
            };

            _dbConfig.Tags.Update(uTag);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;
            await _dbConfig.LogAuditAsync(
                "Update",
                "Tag",
                uTag.Id,
                uTag.Name,
                userId,
                commercialUserId,
                oldValues,
                newValues,
                $"تم تعديل القسم: {uTag.Name}"
            );

            return Ok(new GlobalResponse<Tag>
            {
                Data = uTag,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial")]
        [HttpDelete("DeleteTag")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteTag(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<int>
                {
                    Data = 0,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var tag = await _dbConfig.Tags
                .Include(t => t.User)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User!.Id == userInsertByUserId || x.User.InsertByUserId == userId));
            if (tag == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "tag not exsit"
                });
            }

            var children = await _dbConfig.Tags.Where(x => x.ParentTagId == id && x.IsDeleted == false).ToListAsync();
            foreach (var child in children)
            {
                child.IsDeleted = true;
                child.UpdateDate = DateTime.UtcNow;
                _dbConfig.Tags.Update(child);
            }

            tag!.IsDeleted = true;
            tag.UpdateDate = DateTime.UtcNow;
            _dbConfig.Tags.Update(tag);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Tag>
            {
                Data = tag,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpGet("GetTags")]
        public ActionResult<GlobalResponse<PagedList<Tag>>> GetTags(int pageNumber, int pageSize, string? info)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<PagedList<Tag>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var tag = _dbConfig.Tags.Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId)).AsQueryable();

            if (info != null)
            {
                tag = tag.Where(x => x.Name.Contains(info));
            }

            var totalItems = tag.Count();

            var pagedResult = new PagedList<Tag>(tag.ToList(), totalItems, pageNumber, pageSize);

            var response = new GlobalResponse<PagedList<Tag>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [Authorize(Roles = "Admin,Commercial")]
        [HttpPost("GenerateCategoriesWithAI")]
        public async Task<ActionResult<GlobalResponse<List<string>>>> GenerateCategoriesWithAI(GenerateCategoriesRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return BadRequest(new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الوصف مطلوب"
                    });
                }

                var apiKey = _configuration["OpenAISettings:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "OpenAI API Key غير موجود في الإعدادات"
                    });
                }

                var maxCategories = Math.Min(Math.Max(request.MaxCategories, 1), 20); // بين 1 و 20
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var currentUser = await _dbConfig.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
                var userInsertByUserId = currentUser?.InsertByUserId ?? userId;

                string? parentCategoryName = null;
                var avoidNames = new List<string>();
                if (request.ExistingCategories != null && request.ExistingCategories.Count > 0)
                    avoidNames.AddRange(request.ExistingCategories.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()));

                if (request.ParentTagId.HasValue)
                {
                    var parent = await _dbConfig.Tags
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(x => x.Id == request.ParentTagId.Value && !x.IsDeleted);
                    if (parent == null)
                    {
                        return BadRequest(new GlobalResponse<List<string>>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "التصنيف الرئيسي غير موجود"
                        });
                    }
                    var parentScoped = parent.InsertByUserId == userId ||
                        (parent.User != null && (parent.User.Id == userInsertByUserId || parent.User.InsertByUserId == userId));
                    if (!parentScoped)
                    {
                        return BadRequest(new GlobalResponse<List<string>>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "لا يمكن استخدام هذا التصنيف كأب"
                        });
                    }
                    if (parent.ParentTagId != null)
                    {
                        return BadRequest(new GlobalResponse<List<string>>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = "يُسمح بمستوين فقط: اختر تصنيفاً رئيسياً (ليس فرعياً)"
                        });
                    }
                    parentCategoryName = parent.Name;
                    var existingSubs = await _dbConfig.Tags.AsNoTracking()
                        .Where(t => t.ParentTagId == parent.Id && !t.IsDeleted && t.InsertByUserId == userId)
                        .Select(t => t.Name)
                        .ToListAsync();
                    foreach (var n in existingSubs)
                    {
                        if (string.IsNullOrEmpty(n)) continue;
                        if (!avoidNames.Contains(n, StringComparer.OrdinalIgnoreCase))
                            avoidNames.Add(n);
                    }
                }

                string prompt;
                if (!string.IsNullOrEmpty(parentCategoryName))
                {
                    prompt = $"أنشئ قائمة بتصنيفات فرعية مناسبة لمطعم، تندرج جميعها تحت التصنيف الرئيسي «{parentCategoryName}».\n\n";
                    prompt += $"استخدم أيضاً السياق التالي من صاحب المطعم:\n{request.Description}\n\n";
                    prompt += "التصنيفات الفرعية يجب أن تكون أسماء أقسام داخلية لهذا القسم الرئيسي فقط (مثل أنواع ضمن «المشروبات» أو «المقبلات»)، وليست أقساماً رئيسية أخرى.\n\n";
                }
                else
                {
                    prompt = $"أنشئ قائمة بأقسام رئيسية مناسبة لمطعم بناءً على الوصف التالي:\n{request.Description}\n\n";
                }

                if (avoidNames.Count > 0)
                {
                    var existingCategoriesList = string.Join("، ", avoidNames.Distinct());
                    prompt += $"الأسماء التالية موجودة بالفعل ولا يجب تكرارها:\n{existingCategoriesList}\n\n";
                    prompt += "أنشئ أسماء جديدة مختلفة عن القائمة أعلاه.\n\n";
                }

                prompt += $"يجب أن تكون الأسماء باللغة العربية ومناسبة لنوع المطعم. أعد قائمة بأسماء التصنيفات فقط بدون شرح أو ترقيم، كل اسم في سطر منفصل. الحد الأقصى: {maxCategories} اسم.";

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 500,
                    temperature = 0.7
                };

                var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API Error: {Error}", errorContent);
                    return StatusCode(500, new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "حدث خطأ أثناء الاتصال بـ OpenAI API"
                    });
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var content = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return BadRequest(new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لم يتم الحصول على استجابة من OpenAI"
                    });
                }

                // Parse the response - split by newlines and clean up
                var categories = content
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim().TrimStart('-', '*', '•', ' '))
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Take(maxCategories)
                    .ToList();

                if (categories.Count == 0)
                {
                    return BadRequest(new GlobalResponse<List<string>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لم يتم العثور على أقسام في الاستجابة"
                    });
                }

                return Ok(new GlobalResponse<List<string>>
                {
                    Data = categories,
                    ErrorStatus = false,
                    Message = "تم إنشاء الأقسام بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating categories with AI");
                return StatusCode(500, new GlobalResponse<List<string>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        private const string TagCategorySeparator = " › ";

        private static bool TagIsInUserScope(Tag tag, int userId, int userInsertByUserId)
        {
            return tag.InsertByUserId == userId ||
                   (tag.User != null && (tag.User.Id == userInsertByUserId || tag.User.InsertByUserId == userId));
        }

        /// <summary>يحدد نص حقل Tags للأطباق عند توليدها ضمن تصنيف محدد.</summary>
        private async Task<(bool Ok, string? ErrorMessage, string? FixedCategoryPath)> ResolveAiItemsFixedCategoryAsync(
            int userId, int? rootTagId, int? subTagId)
        {
            if (!rootTagId.HasValue)
                return (true, null, null);

            var currentUser = await _dbConfig.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            var userInsertByUserId = currentUser?.InsertByUserId ?? userId;

            var root = await _dbConfig.Tags.Include(t => t.User).FirstOrDefaultAsync(x => x.Id == rootTagId.Value && !x.IsDeleted);
            if (root == null)
                return (false, "التصنيف الرئيسي غير موجود", null);
            if (!TagIsInUserScope(root, userId, userInsertByUserId))
                return (false, "لا يمكن استخدام هذا القسم", null);
            if (root.ParentTagId != null)
                return (false, "اختر تصنيفاً رئيسياً فقط (ليس فرعياً)", null);

            var hasChildren = await _dbConfig.Tags.AnyAsync(t => t.ParentTagId == root.Id && !t.IsDeleted);

            if (hasChildren)
            {
                if (!subTagId.HasValue)
                    return (false, "هذا القسم يحتوي تصنيفات فرعية — اختر قسماً فرعياً", null);

                var sub = await _dbConfig.Tags.Include(t => t.User).FirstOrDefaultAsync(x => x.Id == subTagId.Value && !x.IsDeleted);
                if (sub == null)
                    return (false, "التصنيف الفرعي غير موجود", null);
                if (sub.ParentTagId != root.Id)
                    return (false, "التصنيف الفرعي لا يتبع القسم الرئيسي المختار", null);
                if (!TagIsInUserScope(sub, userId, userInsertByUserId))
                    return (false, "لا يمكن استخدام هذا القسم الفرعي", null);

                var rootName = root.Name ?? "";
                var subName = sub.Name ?? "";
                return (true, null, $"{rootName}{TagCategorySeparator}{subName}");
            }

            if (subTagId.HasValue)
                return (false, "هذا القسم الرئيسي بلا أقسام فرعية — أزل اختيار التصنيف الفرعي", null);

            return (true, null, root.Name ?? "");
        }

        [Authorize(Roles = "Admin,Commercial")]
        [HttpPost("GenerateItemsWithAI")]
        public async Task<ActionResult<GlobalResponse<List<GeneratedItemDto>>>> GenerateItemsWithAI(GenerateItemsRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return BadRequest(new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الوصف مطلوب"
                    });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var scopeResult = await ResolveAiItemsFixedCategoryAsync(userId, request.RootTagId, request.SubTagId);
                if (!scopeResult.Ok)
                {
                    return BadRequest(new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = scopeResult.ErrorMessage ?? "تعذر تحديد القسم"
                    });
                }

                var fixedCategoryPath = scopeResult.FixedCategoryPath;

                var apiKey = _configuration["OpenAISettings:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "OpenAI API Key غير موجود في الإعدادات"
                    });
                }

                var maxItems = Math.Min(Math.Max(request.MaxItems, 1), 20);

                string prompt;
                if (!string.IsNullOrEmpty(fixedCategoryPath))
                {
                    prompt = $"أنشئ قائمة بأطباق ومشروبات مناسبة لمطعم، تندرج جميعها تحت القسم «{fixedCategoryPath}».\n\n";
                    prompt += $"سياق إضافي من صاحب المطعم:\n{request.Description}\n\n";
                    prompt += "ركز على أصناف منطقية لهذا القسم فقط.\n\n";
                }
                else
                {
                    prompt = $"أنشئ قائمة بأطباق ومشروبات مناسبة لمطعم بناءً على الوصف التالي:\n{request.Description}\n\n";
                }

                if (request.ExistingItems != null && request.ExistingItems.Count > 0)
                {
                    var existingItemsList = string.Join(", ", request.ExistingItems.Select(i => i.Name));
                    prompt += $"الأطباق التالية موجودة بالفعل ولا يجب تكرارها:\n{existingItemsList}\n\n";
                    prompt += "أنشئ أطباقاً جديدة مختلفة عن الأسماء أعلاه.\n\n";
                }

                if (!string.IsNullOrEmpty(fixedCategoryPath))
                {
                    prompt += $"يجب أن تكون الأسماء بالعربية. أعد كل طبق في سطر بالشكل التالي (بدون عمود قسم):\nاسم الطبق | السعر (رقم فقط بدون فواصل) | وصف قصير اختياري\nمثال: عصير برتقال طازج | 2500 | عصير طبيعي\nالحد الأقصى: {maxItems} طبق.";
                }
                else
                {
                    prompt += $"يجب أن تكون الأطباق باللغة العربية ومناسبة لنوع المطعم. أعد قائمة بكل طبق في سطر منفصل بالشكل التالي:\nاسم الطبق | القسم | السعر (بالأرقام فقط بدون عملة) | الوصف (اختياري)\nمثال: حمص | مقبلات | 3000 | طبق حمص تقليدي من المطبخ العراقي\nالحد الأقصى للأطباق: {maxItems} طبق.";
                }

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API Error: {Error}", errorContent);
                    return StatusCode(500, new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "حدث خطأ أثناء الاتصال بـ OpenAI API"
                    });
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var content = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return BadRequest(new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لم يتم الحصول على استجابة من OpenAI"
                    });
                }

                // Parse the response
                var items = new List<GeneratedItemDto>();
                var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines.Take(maxItems))
                {
                    var trimmedLine = line.Trim().TrimStart('-', '*', '•', ' ');
                    if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                    var parts = trimmedLine.Split('|');
                    if (parts.Length < 2) continue;

                    if (!string.IsNullOrEmpty(fixedCategoryPath))
                    {
                        var item = new GeneratedItemDto
                        {
                            Category = fixedCategoryPath,
                            Name = parts[0].Trim()
                        };
                        if (string.IsNullOrWhiteSpace(item.Name)) continue;

                        if (decimal.TryParse(parts[1].Trim().Replace(",", ""), System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var priceFixed))
                        {
                            item.SellingPrice = priceFixed;
                            item.DisCountPrice = priceFixed;
                            item.PurchasingPrice = priceFixed * 0.6m;
                            item.Description = parts.Length >= 3
                                ? string.Join("|", parts.Skip(2)).Trim()
                                : null;
                        }
                        else if (parts.Length >= 4 &&
                                 decimal.TryParse(parts[2].Trim().Replace(",", ""), System.Globalization.NumberStyles.Any,
                                     System.Globalization.CultureInfo.InvariantCulture, out var priceAlt))
                        {
                            item.SellingPrice = priceAlt;
                            item.DisCountPrice = priceAlt;
                            item.PurchasingPrice = priceAlt * 0.6m;
                            item.Description = parts.Length > 3 ? parts[3].Trim() : null;
                        }
                        else
                        {
                            item.SellingPrice = 0;
                            item.DisCountPrice = 0;
                            item.PurchasingPrice = 0;
                            item.Description = parts.Length >= 2 ? string.Join("|", parts.Skip(1)).Trim() : null;
                        }

                        items.Add(item);
                        continue;
                    }

                    var itemFree = new GeneratedItemDto
                    {
                        Name = parts[0].Trim(),
                        Category = parts.Length > 1 ? parts[1].Trim() : "مواد اخرى",
                        Description = parts.Length > 3 ? parts[3].Trim() : null
                    };

                    if (parts.Length > 2 && decimal.TryParse(parts[2].Trim().Replace(",", ""), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var price))
                    {
                        itemFree.SellingPrice = price;
                        itemFree.DisCountPrice = price;
                        itemFree.PurchasingPrice = price * 0.6m;
                    }
                    else
                    {
                        itemFree.SellingPrice = 0;
                        itemFree.DisCountPrice = 0;
                        itemFree.PurchasingPrice = 0;
                    }

                    items.Add(itemFree);
                }

                if (items.Count == 0)
                {
                    return BadRequest(new GlobalResponse<List<GeneratedItemDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لم يتم العثور على أطباق في الاستجابة"
                    });
                }

                return Ok(new GlobalResponse<List<GeneratedItemDto>>
                {
                    Data = items,
                    ErrorStatus = false,
                    Message = "تم إنشاء الأطباق بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating items with AI");
                return StatusCode(500, new GlobalResponse<List<GeneratedItemDto>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Admin,Commercial")]
        [HttpPost("AddMultipleItems")]
        public async Task<ActionResult<GlobalResponse<List<Item>>>> AddMultipleItems(List<GeneratedItemDto> items)
        {
            try
            {
                if (items == null || items.Count == 0)
                {
                    return BadRequest(new GlobalResponse<List<Item>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لا توجد أطباق للحفظ"
                    });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var savedItems = new List<Item>();
                var errors = new List<string>();

                foreach (var itemDto in items)
                {
                    if (string.IsNullOrWhiteSpace(itemDto.Name))
                    {
                        errors.Add("اسم الطبق مطلوب");
                        continue;
                    }

                    // Check if item already exists
                    var existingItem = await _dbConfig.Items.FirstOrDefaultAsync(
                        x => x.Name == itemDto.Name && 
                        x.IsDeleted == false && 
                        x.InsertByUserId == userId);

                    if (existingItem != null)
                    {
                        errors.Add($"الطبق '{itemDto.Name}' موجود بالفعل");
                        continue;
                    }

                    var newItem = new Item
                    {
                        Name = itemDto.Name,
                        Description = itemDto.Description,
                        SellingPrice = itemDto.SellingPrice,
                        PurchasingPrice = itemDto.PurchasingPrice,
                        DisCountPrice = itemDto.DisCountPrice,
                        Tags = itemDto.Category,
                        IsAvailable = true,
                        Code = $"ITEM{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}",
                        InsertByUserId = userId,
                        InsertDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        IsDeleted = false
                    };

                    _dbConfig.Items.Add(newItem);
                    savedItems.Add(newItem);
                }

                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<List<Item>>
                {
                    Data = savedItems,
                    ErrorStatus = false,
                    Message = errors.Count > 0 
                        ? $"تم حفظ {savedItems.Count} طبق بنجاح. {string.Join(", ", errors)}"
                        : $"تم حفظ {savedItems.Count} طبق بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding multiple items");
                return StatusCode(500, new GlobalResponse<List<Item>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Admin,Commercial")]
        [HttpPost("AddMultipleTags")]
        public async Task<ActionResult<GlobalResponse<List<Tag>>>> AddMultipleTags(List<TagRequset> tags)
        {
            try
            {
                if (tags == null || tags.Count == 0)
                {
                    return BadRequest(new GlobalResponse<List<Tag>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لا توجد أقسام للحفظ"
                    });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var savedTags = new List<Tag>();
                var errors = new List<string>();

                foreach (var tagRequest in tags)
                {
                    if (string.IsNullOrWhiteSpace(tagRequest.Name))
                    {
                        errors.Add("اسم القسم مطلوب");
                        continue;
                    }

                    // Check if tag already exists (same name under same parent)
                    var existingTag = await _dbConfig.Tags.FirstOrDefaultAsync(
                        x => x.Name == tagRequest.Name && 
                        x.IsDeleted == false && 
                        x.InsertByUserId == userId &&
                        x.ParentTagId == tagRequest.ParentTagId);

                    if (existingTag != null)
                    {
                        errors.Add($"القسم '{tagRequest.Name}' موجود بالفعل");
                        continue;
                    }

                    var newTag = _mapper.Map<Tag>(tagRequest);
                    newTag.InsertByUserId = userId;
                    _dbConfig.Tags.Add(newTag);
                    savedTags.Add(newTag);
                }

                await _dbConfig.SaveChangesAsync();

                return Ok(new GlobalResponse<List<Tag>>
                {
                    Data = savedTags,
                    ErrorStatus = false,
                    Message = errors.Count > 0 
                        ? $"تم حفظ {savedTags.Count} قسم بنجاح. {string.Join(", ", errors)}"
                        : $"تم حفظ {savedTags.Count} قسم بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding multiple tags");
                return StatusCode(500, new GlobalResponse<List<Tag>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        // add item 
        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("AddItem")]
        public async Task<ActionResult<GlobalResponse<Item>>> AddItem([FromForm] ItemRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var itemCode = request.Code ?? RandomCode();
            var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Name == request.Name && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId));
            if (item != null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = item,
                    ErrorStatus = true,
                    Message = "Item is already exsit"
                });
            }
            var newItem = _mapper.Map<Item>(request);
            if(request.Image != null)
            {
                newItem.Image = await UploadIamgesAsync(request.Image);
            }
            newItem.Code = itemCode;
            newItem.InsertByUserId = userId;
            _dbConfig.Items.Add(newItem);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Item>
            {
                Data = newItem,
                ErrorStatus = false,
                Message = "done"
            });
        }

        // update item 
        [Authorize(Roles = "Commercial")]
        [HttpPut("UpdateItem")]
        public async Task<ActionResult<GlobalResponse<Item>>> UpdateItem([FromForm]  ItemRequest request, int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
            if (item == null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = item,
                    ErrorStatus = true,
                    Message = "user not exsit"
                });
            }
           

            // Store old values for audit log
            var oldValues = new
            {
                Name = item.Name,
                Code = item.Code,
                SellingPrice = item.SellingPrice,
                PurchasingPrice = item.PurchasingPrice,
                DisCountPrice = item.DisCountPrice,
                Description = item.Description,
                Tags = item.Tags,
                IsAvailable = item.IsAvailable,
                Image = item.Image
            };

            item.Tags = request.Tags;
            item.PurchasingPrice = request.PurchasingPrice;
            item.DisCountPrice = request.DisCountPrice;
            item.Description = request.Description;
            item.SellingPrice = request.SellingPrice;
            item.IsAvailable = request.IsAvailable;
            item.Code = request.Code;
            item.Name = request.Name;
            item.Image = request.Image != null ? await UploadIamgesAsync(request.Image): item.Image;

            // Store new values for audit log
            var newValues = new
            {
                Name = item.Name,
                Code = item.Code,
                SellingPrice = item.SellingPrice,
                PurchasingPrice = item.PurchasingPrice,
                DisCountPrice = item.DisCountPrice,
                Description = item.Description,
                Tags = item.Tags,
                IsAvailable = item.IsAvailable,
                Image = item.Image
            };

            _dbConfig.Items.Update(item);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;
            await _dbConfig.LogAuditAsync(
                "Update",
                "Item",
                item.Id,
                item.Name,
                userId,
                commercialUserId,
                oldValues,
                newValues,
                $"تم تعديل الصنف: {item.Name}"
            );

            return Ok(new GlobalResponse<Item>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial")]
        [HttpDelete("DeleteItem")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteItem(int id)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);


            var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
            if (item == null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "item not exsit"
                });
            }

            item!.IsDeleted = true;
            _dbConfig.Items.Update(item);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;
            await _dbConfig.LogAuditAsync(
                "Delete",
                "Item",
                item.Id,
                item.Name,
                userId,
                commercialUserId,
                null,
                null,
                $"تم حذف الصنف: {item.Name}"
            );

            return Ok(new GlobalResponse<Item>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }


        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpGet("GetItems")]
        public ActionResult<GlobalResponse<PagedList<Item>>> GetItems(int pageNumber, int pageSize, string? info)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<PagedList<Item>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var item = _dbConfig.Items
                .Include(x => x.User)
                .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || (x.User != null && x.User.Id == userInsertByUserId) || (x.User != null && x.User.InsertByUserId == userId)))
                .AsQueryable();

            if (info != null)
            {
                item = item.Where(x => x.Code == info || x.Name.Contains(info) || x.Description!.Contains(info) || x.Tags!.Contains(info));
            }

            var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";

            var totalItems = item.Count();
            var itemList = item.ToList();
            
            foreach(var n in itemList)
            {
                if (!string.IsNullOrEmpty(n.Image))
            {
                n.Image = imageBaseUrl + n.Image;
            }
            }

            var pagedResult = new PagedList<Item>(itemList, totalItems, pageNumber, pageSize);

            var response = new GlobalResponse<PagedList<Item>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [Authorize(Roles = "Commercial,POS,Reader,Waiter")]
        [HttpGet("GetItemsByCode")]
        public async Task<ActionResult<GlobalResponse<Object>>> GetItemsByCode(string code)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);


            var item =await _dbConfig.Items.Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId) && x.Code == code).FirstOrDefaultAsync();



            if (item == null)
            {
                return NotFound(new GlobalResponse<Object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "Item not found"
                });
            }

            var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";

            item.Image = imageBaseUrl + item.Image;
            
            var response = new GlobalResponse<Object>
            {
                Data = item,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpPost("AddOrder")]
        public async Task<ActionResult<GlobalResponse<CustomerOrder>>> AddOrder(CustomerOrderRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return Unauthorized(new GlobalResponse<CustomerOrder>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                if (request.CustomerOrderItem == null || !request.CustomerOrderItem.Any())
                {
                    return BadRequest(new GlobalResponse<CustomerOrder>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Order must contain at least one item"
                    }); 

                }
                
                // Load items with user information to avoid lazy loading issues
                var items = await _dbConfig.Items
                    .Include(x => x.User)
                    .Where(x => !x.IsDeleted && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId))
                    .ToListAsync();

                var orderCode = request.OrderCode ?? RandomCode();
                var orderType = request.OrderType ?? "DineIn";
                
                // Calculate DailySequenceNumber for all orders (resets daily)
                int? dailySequenceNumber = null;
                try
                {
                    var commercialUserId = GetCommercialUserId();
                    var today = DateTime.UtcNow.Date;
                    var tomorrow = today.AddDays(1);

                    // Get max daily sequence for current business day
                    var ordersToday = await _dbConfig.CustomerOrders
                        .Where(o => o.InsertByUserId == commercialUserId
                            && o.InsertDate >= today
                            && o.InsertDate < tomorrow
                            && o.DailySequenceNumber.HasValue
                            && !o.IsDeleted)
                        .Select(o => o.DailySequenceNumber.Value)
                        .ToListAsync();

                    var maxSequenceToday = ordersToday.Any() ? ordersToday.Max() : 0;
                    dailySequenceNumber = maxSequenceToday + 1;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating DailySequenceNumber");
                    dailySequenceNumber = null;
                }

                // Handle Delivery Driver
                int? deliveryDriverId = null;
                if (orderType == "Delivery")
                {
                    if (request.DeliveryDriverId.HasValue)
                    {
                        // Use existing driver
                        var existingDriver = await _dbConfig.DeliveryDrivers
                            .FirstOrDefaultAsync(d => d.Id == request.DeliveryDriverId.Value 
                                && !d.IsDeleted 
                                && d.IsActive);
                        
                        if (existingDriver != null)
                        {
                            deliveryDriverId = existingDriver.Id;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(request.NewDriverName) 
                        && !string.IsNullOrWhiteSpace(request.NewDriverPhone))
                    {
                        // Create new driver
                        var commercialUserId = GetCommercialUserId();
                        var newDriver = new DeliveryDriver
                        {
                            Name = request.NewDriverName.Trim(),
                            PhoneNumber = request.NewDriverPhone.Trim(),
                            Address = request.NewDriverAddress?.Trim(),
                            VehicleType = request.NewDriverVehicleType?.Trim(),
                            VehicleNumber = request.NewDriverVehicleNumber?.Trim(),
                            IsActive = true,
                            InsertByUserId = commercialUserId,
                            InsertDate = DateTime.UtcNow,
                            UpdateDate = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        
                        _dbConfig.DeliveryDrivers.Add(newDriver);
                        await _dbConfig.SaveChangesAsync();
                        deliveryDriverId = newDriver.Id;
                    }
                }

                var newOrder = new CustomerOrder
                {
                    OrderCode = orderCode,
                    PaymentMethod = request.PaymentMethod ?? "Cash",
                    InsertByUserId = userId,
                    TableId = request.TableId,
                    ReservationId = request.ReservationId,
                    OrderType = orderType,
                    Notes = request.Notes,
                    PagerNumber = request.PagerNumber,
                    OrderStatus = "Pending",
                    PaymentStatus = "Pending",
                    DailySequenceNumber = dailySequenceNumber,
                    DeliveryDriverId = deliveryDriverId,
                    DeliveryStatus = orderType == "Delivery" ? (request.DeliveryStatus ?? "Pending") : null,
                    DeliveryAddress = request.DeliveryAddress,
                    DeliveryPhoneNumber = request.DeliveryPhoneNumber,
                    DeliveryCustomerName = request.DeliveryCustomerName,
                    DeliveryFee = request.DeliveryFee,
                    DiscountType = request.DiscountType,
                    DiscountValue = request.DiscountValue,
                    DiscountAmount = request.DiscountAmount,
                    DiscountPercent = request.DiscountPercent,
                    OrderSubTotal = request.OrderSubTotal,
                    OrderTotalAfterDiscount = request.OrderTotalAfterDiscount,
                    DeliveryAssignedAt = deliveryDriverId.HasValue ? DateTime.UtcNow : null
                };
                _dbConfig.CustomerOrders.Add(newOrder);
                await _dbConfig.SaveChangesAsync();

                if (request.CustomerOrderItem != null && request.CustomerOrderItem.Any())
                {
                    var insertItems = new List<CustomerOrderItem>();
                    var itemIds = request.CustomerOrderItem.Select(x => x.ItemId).Distinct().ToList();
                    
                    // Validate all items exist before processing
                    var invalidItemIds = itemIds.Where(id => !items.Any(x => x.Id == id)).ToList();
                    if (invalidItemIds.Any())
                    {
                        _logger.LogWarning("Invalid item IDs in order: {ItemIds}", string.Join(", ", invalidItemIds));
                        return BadRequest(new GlobalResponse<CustomerOrder>
                        {
                            Data = null,
                            ErrorStatus = true,
                            Message = $"Invalid item IDs: {string.Join(", ", invalidItemIds)}"
                        });
                    }

                    // Check item availability before processing
                    foreach (var itemRequest in request.CustomerOrderItem)
                    {
                        var currentItem = items.FirstOrDefault(x => x.Id == itemRequest.ItemId);
                        if (currentItem == null) continue;

                        // Check if item is available
                        if (!currentItem.IsAvailable)
                        {
                            _logger.LogWarning("Item {ItemId} is not available", itemRequest.ItemId);
                            return BadRequest(new GlobalResponse<CustomerOrder>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"Item '{currentItem.Name}' is not available"
                            });
                        }
                    }

                    foreach (var itemRequest in request.CustomerOrderItem)
                    {
                        var existingItem = insertItems.FirstOrDefault(x => x.ItemId == itemRequest.ItemId);
                        if (existingItem != null)
                        {
                            // Increment the quantity of an existing item
                            existingItem.Quantity += itemRequest.Quantity;
                        }
                        else
                        {
                            var currentItem = items.FirstOrDefault(x => x.Id == itemRequest.ItemId);
                            if (currentItem == null)
                            {
                                _logger.LogWarning("Item not found: {ItemId}", itemRequest.ItemId);
                                return BadRequest(new GlobalResponse<CustomerOrder>
                                {
                                    Data = null,
                                    ErrorStatus = true,
                                    Message = $"Item with ID {itemRequest.ItemId} not found"
                                });
                            }

                            // Use discount price if available, otherwise use selling price
                            var finalPrice = currentItem.DisCountPrice > 0 && currentItem.DisCountPrice != currentItem.SellingPrice
                                ? currentItem.DisCountPrice
                                : currentItem.SellingPrice;

                            var newOrderItem = new CustomerOrderItem
                            {
                                CustomerOrderId = newOrder.Id,
                                SellingPrice = finalPrice,
                                PurchasingPrice = currentItem.PurchasingPrice,
                                Quantity = itemRequest.Quantity,
                                ItemId = itemRequest.ItemId,
                                InsertByUserId = userId,
                            };

                            insertItems.Add(newOrderItem);
                        }
                    }

                    _dbConfig.CustomerOrderItems.AddRange(insertItems);
                    await _dbConfig.SaveChangesAsync();


                    // Handle multiple tables (TableIds) or single table (TableId)
                    // Only process tables for DineIn orders
                    if (newOrder.OrderType == "DineIn" || string.IsNullOrEmpty(newOrder.OrderType))
                    {
                        var commercialUserId = GetCommercialUserId();
                        var tablesToUpdate = new List<Table>();
                        
                        if (request.TableIds != null && request.TableIds.Any())
                        {
                            // Multiple tables - create OrderTable entries
                            var orderTables = new List<OrderTable>();
                            var isFirst = true;
                            
                            foreach (var tableId in request.TableIds.Distinct())
                            {
                                var table = await _dbConfig.Tables
                                    .FirstOrDefaultAsync(t => t.Id == tableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);
                                
                                if (table != null)
                                {
                                    // Create OrderTable entry
                                    var orderTable = new OrderTable
                                    {
                                        OrderId = newOrder.Id,
                                        TableId = tableId,
                                        IsPrimary = isFirst,
                                        InsertByUserId = userId
                                    };
                                    orderTables.Add(orderTable);
                                    
                                    // Update table status
                                    table.Status = "Occupied";
                                    table.CurrentOrderId = newOrder.Id;
                                    tablesToUpdate.Add(table);
                                    
                                    isFirst = false;
                                }
                            }
                            
                            if (orderTables.Any())
                            {
                                _dbConfig.OrderTables.AddRange(orderTables);
                                await _dbConfig.SaveChangesAsync();
                                
                                // Set TableId to the first table for backward compatibility
                                newOrder.TableId = request.TableIds.First();
                                _dbConfig.CustomerOrders.Update(newOrder);
                                await _dbConfig.SaveChangesAsync();
                                
                                // Update all tables
                                foreach (var table in tablesToUpdate)
                                {
                                    _dbConfig.Tables.Update(table);
                                }
                                await _dbConfig.SaveChangesAsync();
                                
                            // Send SignalR notifications for all tables
                            foreach (var table in tablesToUpdate)
                            {
                                await _hubContext.Clients.All.SendAsync("TableUpdated", new
                                {
                                    TableId = table.Id,
                                    Status = table.Status,
                                    TableNumber = table.TableNumber,
                                    Zone = table.Zone,
                                    CurrentOrderId = newOrder.Id
                                });
                            }
                            
                            // If merged tables and order is from POS (not Waiter), return all merged tables to Available
                            // This allows the tables to be used again immediately after order completion in POS
                            if (request.TableIds != null && request.TableIds.Count > 1 && 
                                (user.Role == "POS" || user.Role == "Commercial") && 
                                (newOrder.OrderType == "DineIn" || string.IsNullOrEmpty(newOrder.OrderType)))
                            {
                                foreach (var table in tablesToUpdate)
                                {
                                    table.Status = "Available";
                                    table.CurrentOrderId = null;
                                    _dbConfig.Tables.Update(table);
                                }
                                await _dbConfig.SaveChangesAsync();
                                
                                // Send SignalR notifications for all tables returning to Available
                                foreach (var table in tablesToUpdate)
                                {
                                    await _hubContext.Clients.All.SendAsync("TableUpdated", new
                                    {
                                        TableId = table.Id,
                                        Status = table.Status,
                                        TableNumber = table.TableNumber,
                                        Zone = table.Zone,
                                        CurrentOrderId = (int?)null
                                    });
                                }
                            }
                            }
                        }
                        else if (newOrder.TableId.HasValue)
                    {
                            // Single table - backward compatibility
                        var table = await _dbConfig.Tables.FirstOrDefaultAsync(t => t.Id == newOrder.TableId.Value);
                        if (table != null)
                        {
                            table.Status = "Occupied";
                            table.CurrentOrderId = newOrder.Id;
                            _dbConfig.Tables.Update(table);
                                await _dbConfig.SaveChangesAsync();
                                
                                // Create OrderTable entry for consistency
                                var orderTable = new OrderTable
                                {
                                    OrderId = newOrder.Id,
                                    TableId = table.Id,
                                    IsPrimary = true,
                                    InsertByUserId = userId
                                };
                                _dbConfig.OrderTables.Add(orderTable);
                            await _dbConfig.SaveChangesAsync();
                            
                            // Send SignalR notification for table update
                            await _hubContext.Clients.All.SendAsync("TableUpdated", new
                            {
                                TableId = table.Id,
                                Status = table.Status,
                                    TableNumber = table.TableNumber,
                                    Zone = table.Zone,
                                    CurrentOrderId = newOrder.Id
                            });
                            }
                        }
                    }
                }

                _logger.LogInformation("Order created successfully: {OrderCode} by user {UserId}", orderCode, userId);
                
                // Send SignalR notification for new order
                try
                {
                    await _hubContext.Clients.All.SendAsync("OrderAdded", new
                    {
                        OrderId = newOrder.Id,
                        OrderCode = newOrder.OrderCode,
                        TableId = newOrder.TableId,
                        OrderType = newOrder.OrderType
                    });
                    _logger.LogInformation("SignalR notification sent for OrderAdded: OrderId={OrderId}, TableId={TableId}", newOrder.Id, newOrder.TableId);

                    // Also send PublicOrderAdded for Takeaway/Delivery orders
                    if (newOrder.OrderType == "Takeaway" || newOrder.OrderType == "Delivery")
                    {
                        var commercialUserId = GetCommercialUserId();
                        await _hubContext.Clients.All.SendAsync("PublicOrderAdded", new
                        {
                            CommercialUserId = commercialUserId,
                            OrderId = newOrder.Id,
                            OrderCode = newOrder.OrderCode,
                            OrderType = newOrder.OrderType,
                            DailySequenceNumber = newOrder.DailySequenceNumber,
                            InsertDate = newOrder.InsertDate
                        });
                        _logger.LogInformation("SignalR notification sent for PublicOrderAdded: OrderId={OrderId}, CommercialUserId={CommercialUserId}", newOrder.Id, commercialUserId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SignalR notification for OrderAdded");
                }

                // Return a simple response without navigation properties to avoid serialization issues
                var responseData = new
                {
                    Id = newOrder.Id,
                    OrderCode = newOrder.OrderCode,
                    PaymentMethod = newOrder.PaymentMethod,
                    OrderType = newOrder.OrderType,
                    TableId = newOrder.TableId,
                    ReservationId = newOrder.ReservationId,
                    InsertDate = newOrder.InsertDate
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = responseData,
                    ErrorStatus = false,
                    Message = "Order added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new GlobalResponse<CustomerOrder>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "An error occurred while creating the order"
                });
            }
        }

        [Authorize(Roles = "Commercial")]
        [HttpPut("UpdateOrder/{id}")]
        public async Task<ActionResult<GlobalResponse<CustomerOrder>>> UpdateOrder(int id, CustomerOrderRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                if (user == null)
                {
                    return BadRequest(new GlobalResponse<CustomerOrder>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                var userInsertByUserId = user.InsertByUserId;

                // Get existing order
                var existingOrder = await _dbConfig.CustomerOrders
                    .Include(x => x.CustomerOrderItem)
                    .ThenInclude(x => x.Item)
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && 
                        (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId));

                if (existingOrder == null)
                {
                    return NotFound(new GlobalResponse<CustomerOrder>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الفاتورة غير موجودة"
                    });
                }

                // Store old values for audit log (before any changes)
                var oldItemsCount = existingOrder.CustomerOrderItem?.Count ?? 0;
                var oldOrderValues = new
                {
                    PaymentMethod = existingOrder.PaymentMethod,
                    OrderType = existingOrder.OrderType,
                    OrderStatus = existingOrder.OrderStatus,
                    PaymentStatus = existingOrder.PaymentStatus,
                    TableId = existingOrder.TableId,
                    ReservationId = existingOrder.ReservationId,
                    Notes = existingOrder.Notes,
                    PagerNumber = existingOrder.PagerNumber,
                    DiscountType = existingOrder.DiscountType,
                    DiscountValue = existingOrder.DiscountValue,
                    DiscountAmount = existingOrder.DiscountAmount,
                    DiscountPercent = existingOrder.DiscountPercent,
                    OrderSubTotal = existingOrder.OrderSubTotal,
                    OrderTotalAfterDiscount = existingOrder.OrderTotalAfterDiscount,
                    ItemsCount = oldItemsCount
                };

                // Update order basic info
                existingOrder.PaymentMethod = request.PaymentMethod;
                existingOrder.OrderType = request.OrderType;
                existingOrder.Notes = request.Notes;
                existingOrder.PagerNumber = request.PagerNumber;
                existingOrder.TableId = request.TableId;
                existingOrder.ReservationId = request.ReservationId;
                existingOrder.DiscountType = request.DiscountType;
                existingOrder.DiscountValue = request.DiscountValue;
                existingOrder.DiscountAmount = request.DiscountAmount;
                existingOrder.DiscountPercent = request.DiscountPercent;
                existingOrder.OrderSubTotal = request.OrderSubTotal;
                existingOrder.OrderTotalAfterDiscount = request.OrderTotalAfterDiscount;

                // Handle order items update
                // Remove old order items
                _dbConfig.CustomerOrderItems.RemoveRange(existingOrder.CustomerOrderItem);

                // Add new order items
                var newOrderItems = new List<CustomerOrderItem>();

                if (request.CustomerOrderItem != null && request.CustomerOrderItem.Count > 0)
                {
                    foreach (var itemRequest in request.CustomerOrderItem)
                    {
                        var currentItem = await _dbConfig.Items
                            .FirstOrDefaultAsync(x => x.Id == itemRequest.ItemId && x.IsDeleted == false &&
                                (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId));

                        if (currentItem == null)
                        {
                            return BadRequest(new GlobalResponse<CustomerOrder>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"المنتج برقم {itemRequest.ItemId} غير موجود"
                            });
                        }

                        var sellingPrice = currentItem.DisCountPrice > 0 && currentItem.DisCountPrice < currentItem.SellingPrice
                            ? currentItem.DisCountPrice
                            : currentItem.SellingPrice;

                        var newOrderItem = new CustomerOrderItem
                        {
                            ItemId = itemRequest.ItemId,
                            Quantity = itemRequest.Quantity,
                            SellingPrice = sellingPrice,
                            CustomerOrderId = existingOrder.Id,
                            InsertByUserId = userId,
                            InsertDate = DateTime.Now
                        };

                        newOrderItems.Add(newOrderItem);
                    }

                    _dbConfig.CustomerOrderItems.AddRange(newOrderItems);
                }

                _dbConfig.CustomerOrders.Update(existingOrder);
                await _dbConfig.SaveChangesAsync();

                // Reload order items to get accurate count after save
                await _dbConfig.Entry(existingOrder)
                    .Collection(x => x.CustomerOrderItem)
                    .LoadAsync();

                // Log audit for order update
                var commercialUserId = user.Role == "Commercial" ? userId : user.InsertByUserId;
                var newItemsCount = existingOrder.CustomerOrderItem?.Count ?? 0;
                var newOrderValues = new
                {
                    PaymentMethod = existingOrder.PaymentMethod,
                    OrderType = existingOrder.OrderType,
                    OrderStatus = existingOrder.OrderStatus,
                    PaymentStatus = existingOrder.PaymentStatus,
                    TableId = existingOrder.TableId,
                    ReservationId = existingOrder.ReservationId,
                    Notes = existingOrder.Notes,
                    PagerNumber = existingOrder.PagerNumber,
                    DiscountType = existingOrder.DiscountType,
                    DiscountValue = existingOrder.DiscountValue,
                    DiscountAmount = existingOrder.DiscountAmount,
                    DiscountPercent = existingOrder.DiscountPercent,
                    OrderSubTotal = existingOrder.OrderSubTotal,
                    OrderTotalAfterDiscount = existingOrder.OrderTotalAfterDiscount,
                    ItemsCount = newItemsCount
                };

                // Build changes description
                var changesDescription = new List<string>();
                
                if (oldOrderValues.PaymentMethod != newOrderValues.PaymentMethod)
                {
                    changesDescription.Add($"طريقة الدفع: {oldOrderValues.PaymentMethod ?? "---"} → {newOrderValues.PaymentMethod ?? "---"}");
                }
                if (oldOrderValues.OrderType != newOrderValues.OrderType)
                {
                    changesDescription.Add($"نوع الطلب: {oldOrderValues.OrderType ?? "---"} → {newOrderValues.OrderType ?? "---"}");
                }
                if (oldOrderValues.TableId != newOrderValues.TableId)
                {
                    changesDescription.Add($"رقم الطاولة: {oldOrderValues.TableId?.ToString() ?? "---"} → {newOrderValues.TableId?.ToString() ?? "---"}");
                }
                if (oldOrderValues.ReservationId != newOrderValues.ReservationId)
                {
                    changesDescription.Add($"رقم الحجز: {oldOrderValues.ReservationId?.ToString() ?? "---"} → {newOrderValues.ReservationId?.ToString() ?? "---"}");
                }
                if (oldOrderValues.Notes != newOrderValues.Notes)
                {
                    changesDescription.Add("تم تعديل الملاحظات");
                }
                
                // Check if items changed
                if (oldOrderValues.ItemsCount != newOrderValues.ItemsCount)
                {
                    changesDescription.Add($"عدد العناصر: {oldOrderValues.ItemsCount} → {newOrderValues.ItemsCount}");
                }

                // Always log audit - even if no visible changes, we still modified the order
                var description = changesDescription.Count > 0 
                    ? $"تم تعديل الطلب {existingOrder.OrderCode}: {string.Join(", ", changesDescription)}"
                    : $"تم تعديل الطلب {existingOrder.OrderCode}";

                await _dbConfig.LogAuditAsync(
                    "Update",
                    "CustomerOrder",
                    existingOrder.Id,
                    existingOrder.OrderCode,
                    userId,
                    commercialUserId,
                    oldOrderValues,
                    newOrderValues,
                    description
                );

                // Send SignalR notification
                try
                {
                    await _hubContext.Clients.All.SendAsync("OrderUpdated", new
                    {
                        OrderId = existingOrder.Id,
                        OrderCode = existingOrder.OrderCode,
                        TableId = existingOrder.TableId
                    });
                    _logger.LogInformation("SignalR notification sent for OrderUpdated: OrderId={OrderId}", existingOrder.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SignalR notification for OrderUpdated");
                }

                return Ok(new GlobalResponse<CustomerOrder>
                {
                    Data = existingOrder,
                    ErrorStatus = false,
                    Message = "تم تحديث الفاتورة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                return StatusCode(500, new GlobalResponse<CustomerOrder>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء تحديث الفاتورة"
                });
            }
        }

        [Authorize(Roles = "Commercial")]
        [HttpDelete("DeleteOrder")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteOrder(int id)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            var item = await _dbConfig.CustomerOrders.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && x.InsertByUserId == userId);
            if (item == null)
            {
                return BadRequest(new GlobalResponse<CustomerOrder>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "Order not found"
                });
            }

            var orderCode = item.OrderCode;
            item!.IsDeleted = true;
            _dbConfig.CustomerOrders.Update(item);
            await _dbConfig.SaveChangesAsync();

            // Log audit
            var commercialUserId = user?.Role == "Commercial" ? userId : (user?.InsertByUserId ?? userId);
            await _dbConfig.LogAuditAsync(
                "Delete",
                "Order",
                item.Id,
                orderCode,
                userId,
                commercialUserId,
                null,
                null,
                $"تم حذف الطلب: {orderCode}"
            );

            return Ok(new GlobalResponse<CustomerOrder>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpGet("GetTableOrders")]
        public async Task<ActionResult<GlobalResponse<List<OrderDto>>>> GetTableOrders(int tableId)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                // Verify table belongs to this Commercial and get current order
                var table = await _dbConfig.Tables
                    .Include(t => t.CurrentOrder)
                    .ThenInclude(o => o!.CustomerOrderItem)
                    .ThenInclude(oi => oi.Item)
                    .FirstOrDefaultAsync(t => t.Id == tableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);
                
                if (table == null)
                {
                    return NotFound(new GlobalResponse<List<OrderDto>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة غير موجودة"
                    });
                }

                // Get only the current order (if exists)
                var orders = new List<CustomerOrder>();
                if (table.CurrentOrderId.HasValue && table.CurrentOrder != null && !table.CurrentOrder.IsDeleted)
                {
                    orders.Add(table.CurrentOrder);
                }

                if (!orders.Any())
                {
                    return Ok(new GlobalResponse<List<OrderDto>>
                    {
                        Data = new List<OrderDto>(),
                        ErrorStatus = false,
                        Message = "لا يوجد طلب نشط على هذه الطاولة"
                    });
                }

                // Load OrderTables for the current order
                var orderIdsList = orders.Select(o => o.Id).ToList();
                var orderTables = await _dbConfig.OrderTables
                    .Where(ot => orderIdsList.Contains(ot.OrderId) && !ot.IsDeleted)
                    .Include(ot => ot.Table)
                    .ToListAsync();

                // Load all tables that might be needed (for backward compatibility)
                var allTableIds = orders
                    .Where(o => o.TableId.HasValue)
                    .Select(o => o.TableId.Value)
                    .Distinct()
                    .ToList();
                
                var allTablesDict = await _dbConfig.Tables
                    .Where(t => allTableIds.Contains(t.Id) && !t.IsDeleted)
                    .ToDictionaryAsync(t => t.Id);

                var orderDtos = orders.Select(x => {
                    // Get tables for this order from OrderTables
                    var tablesForOrder = orderTables
                        .Where(ot => ot.OrderId == x.Id && ot.Table != null && !ot.Table.IsDeleted)
                        .Select(ot => ot.Table!)
                        .Distinct()
                        .ToList();
                    
                    // If no tables found in OrderTables, check if TableId is set (backward compatibility)
                    if (!tablesForOrder.Any() && x.TableId.HasValue)
                    {
                        if (allTablesDict.TryGetValue(x.TableId.Value, out var singleTable))
                        {
                            tablesForOrder.Add(singleTable);
                        }
                    }
                    
                    // Convert to TableDto to avoid circular reference
                    var tablesDto = tablesForOrder.Select(t => new TableDto
                    {
                        Id = t.Id,
                        TableNumber = t.TableNumber,
                        Capacity = t.Capacity,
                        Status = t.Status,
                        Zone = t.Zone,
                        Notes = t.Notes
                    }).ToList();
                    
                    // Generate merged table numbers string
                    var mergedTableNumbers = tablesDto.Any() 
                        ? string.Join("و", tablesDto.OrderBy(t => t.TableNumber).Select(t => t.TableNumber))
                        : null;

                    return new OrderDto
                    {
                        CustomerOrderItem = x.CustomerOrderItem,
                        OrderPrice = x.CustomerOrderItem?.Sum(item => item.SellingPrice * item.Quantity) ?? 0,
                        OrderCode = x.OrderCode,
                        Id = x.Id,
                        ItemsCount = x.CustomerOrderItem?.Count() ?? 0,
                        DailySequenceNumber = x.DailySequenceNumber,
                        InsertDate = x.InsertDate,
                        CreatedByUserId = x.User != null ? x.User.Id : null,
                        CreatedByUsername = x.User != null ? x.User.Username : null,
                        PaymentMethod = x.PaymentMethod,
                        OrderType = x.OrderType,
                        DiscountType = x.DiscountType,
                        DiscountValue = x.DiscountValue,
                        DiscountAmount = x.DiscountAmount,
                        DiscountPercent = x.DiscountPercent,
                        OrderSubTotal = x.OrderSubTotal,
                        OrderTotalAfterDiscount = x.OrderTotalAfterDiscount,
                        Tables = tablesDto,
                        MergedTableNumbers = mergedTableNumbers
                    };
                }).ToList();

                return Ok(new GlobalResponse<List<OrderDto>>
                {
                    Data = orderDtos,
                    ErrorStatus = false,
                    Message = "تم جلب طلب الطاولة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table orders");
                return StatusCode(500, new GlobalResponse<List<OrderDto>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء جلب طلبات الطاولة"
                });
            }
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpGet("GetMergedTables")]
        public async Task<ActionResult<GlobalResponse<List<int>>>> GetMergedTables(int tableId)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                // Verify table belongs to this Commercial
                var table = await _dbConfig.Tables
                    .FirstOrDefaultAsync(t => t.Id == tableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                if (table == null)
                {
                    return NotFound(new GlobalResponse<List<int>>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة غير موجودة"
                    });
                }

                // Get current order for this table
                if (!table.CurrentOrderId.HasValue)
                {
                    return Ok(new GlobalResponse<List<int>>
                    {
                        Data = new List<int> { tableId },
                        ErrorStatus = false,
                        Message = "لا يوجد طلب نشط على هذه الطاولة"
                    });
                }

                // Get all tables linked to this order via OrderTables
                var mergedTableIds = await _dbConfig.OrderTables
                    .Where(ot => ot.OrderId == table.CurrentOrderId.Value && !ot.IsDeleted)
                    .Select(ot => ot.TableId)
                    .Distinct()
                    .ToListAsync();

                // If no merged tables found, return only this table
                if (!mergedTableIds.Any())
                {
                    mergedTableIds = new List<int> { tableId };
                }

                return Ok(new GlobalResponse<List<int>>
                {
                    Data = mergedTableIds,
                    ErrorStatus = false,
                    Message = "تم جلب الطاولات المدمجة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting merged tables");
                return StatusCode(500, new GlobalResponse<List<int>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء جلب الطاولات المدمجة"
                });
            }
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpPost("MergeTables")]
        public async Task<ActionResult<GlobalResponse<object>>> MergeTables([FromBody] List<int> tableIds)
        {
            try
            {
                if (tableIds == null || tableIds.Count < 2)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يجب اختيار طاولتين على الأقل للدمج"
                    });
                }

                var commercialUserId = GetCommercialUserId();
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

                // Verify all tables belong to this Commercial and get their orders
                var tables = await _dbConfig.Tables
                    .Where(t => tableIds.Contains(t.Id) && !t.IsDeleted && t.InsertByUserId == commercialUserId)
                    .Include(t => t.CurrentOrder)
                    .ThenInclude(o => o!.CustomerOrderItem)
                    .ToListAsync();

                if (tables.Count != tableIds.Count)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "بعض الطاولات غير موجودة أو لا تنتمي لك"
                    });
                }

                // Check which tables have orders
                var tablesWithOrders = tables.Where(t => t.CurrentOrderId.HasValue && t.CurrentOrder != null && !t.CurrentOrder.IsDeleted).ToList();
                CustomerOrder primaryOrder;
                Table primaryTable;

                if (tablesWithOrders.Any())
                {
                    // Use the first table's order as the primary order
                    primaryOrder = tablesWithOrders.First().CurrentOrder!;
                    primaryTable = tablesWithOrders.First();
                }
                else
                {
                    // No orders exist, create a new order for merging empty tables
                    var orderCode = RandomCode();
                    primaryTable = tables.First();
                    
                    primaryOrder = new CustomerOrder
                    {
                        OrderCode = orderCode,
                        PaymentMethod = "Cash",
                        InsertByUserId = userId,
                        TableId = primaryTable.Id,
                        OrderType = "DineIn",
                        OrderStatus = "Pending",
                        PaymentStatus = "Pending"
                    };
                    _dbConfig.CustomerOrders.Add(primaryOrder);
                    await _dbConfig.SaveChangesAsync();
                }

                // If there are multiple orders, merge their items into primary order
                if (tablesWithOrders.Count > 1)
                {
                    // Get existing items in primary order
                    var existingItems = await _dbConfig.CustomerOrderItems
                        .Where(oi => oi.CustomerOrderId == primaryOrder.Id && !oi.IsDeleted)
                        .ToListAsync();

                    // Add items from other orders to primary order
                    foreach (var otherTable in tablesWithOrders.Skip(1))
                    {
                        if (otherTable.CurrentOrder != null && otherTable.CurrentOrder.Id != primaryOrder.Id)
                        {
                            var otherOrderItems = await _dbConfig.CustomerOrderItems
                                .Where(oi => oi.CustomerOrderId == otherTable.CurrentOrder!.Id && !oi.IsDeleted)
                                .ToListAsync();

                            foreach (var item in otherOrderItems)
                            {
                                var existingItem = existingItems.FirstOrDefault(ei => ei.ItemId == item.ItemId);
                                if (existingItem != null)
                                {
                                    // Merge quantities if same item exists
                                    existingItem.Quantity += item.Quantity;
                                    _dbConfig.CustomerOrderItems.Update(existingItem);
                                }
                                else
                                {
                                    // Move item to primary order
                                    item.CustomerOrderId = primaryOrder.Id;
                                    _dbConfig.CustomerOrderItems.Update(item);
                                    existingItems.Add(item);
                                }
                            }

                            // Mark other order as deleted (only if it's different from primary)
                            if (otherTable.CurrentOrder.Id != primaryOrder.Id)
                            {
                                otherTable.CurrentOrder.IsDeleted = true;
                                _dbConfig.CustomerOrders.Update(otherTable.CurrentOrder);
                                
                                // Update table's CurrentOrderId to point to primary order
                                otherTable.CurrentOrderId = primaryOrder.Id;
                                _dbConfig.Tables.Update(otherTable);
                            }
                        }
                    }
                    await _dbConfig.SaveChangesAsync();
                }
                else if (tablesWithOrders.Count == 1)
                {
                    // Only one table has an order, just link other tables to it
                    foreach (var table in tables.Where(t => !tablesWithOrders.Contains(t)))
                    {
                        table.CurrentOrderId = primaryOrder.Id;
                        _dbConfig.Tables.Update(table);
                    }
                    await _dbConfig.SaveChangesAsync();
                }

                // Create OrderTable entries for all tables
                var existingOrderTables = await _dbConfig.OrderTables
                    .Where(ot => ot.OrderId == primaryOrder.Id && !ot.IsDeleted)
                    .ToListAsync();

                var existingTableIds = existingOrderTables.Select(ot => ot.TableId).ToList();

                var orderTablesToAdd = new List<OrderTable>();
                var isFirst = true;
                foreach (var table in tables)
                {
                    if (!existingTableIds.Contains(table.Id))
                    {
                        var orderTable = new OrderTable
                        {
                            OrderId = primaryOrder.Id,
                            TableId = table.Id,
                            IsPrimary = isFirst && table.Id == primaryTable.Id,
                            InsertByUserId = userId
                        };
                        orderTablesToAdd.Add(orderTable);
                    }
                    else if (table.Id == primaryTable.Id)
                    {
                        // Update primary flag
                        var existingOrderTable = existingOrderTables.FirstOrDefault(ot => ot.TableId == table.Id);
                        if (existingOrderTable != null)
                        {
                            existingOrderTable.IsPrimary = true;
                            _dbConfig.OrderTables.Update(existingOrderTable);
                        }
                    }

                    // Update table status
                    table.Status = "Occupied";
                    table.CurrentOrderId = primaryOrder.Id;
                    _dbConfig.Tables.Update(table);
                    isFirst = false;
                }

                if (orderTablesToAdd.Any())
                {
                    _dbConfig.OrderTables.AddRange(orderTablesToAdd);
                }

                // Update primary order TableId for backward compatibility
                primaryOrder.TableId = primaryTable.Id;
                _dbConfig.CustomerOrders.Update(primaryOrder);

                await _dbConfig.SaveChangesAsync();

                // Send SignalR notifications for all tables
                foreach (var table in tables)
                {
                    await _hubContext.Clients.All.SendAsync("TableUpdated", new
                    {
                        TableId = table.Id,
                        Status = table.Status,
                        TableNumber = table.TableNumber,
                        Zone = table.Zone,
                        CurrentOrderId = primaryOrder.Id
                    });
                }

                // Generate merged table numbers
                var mergedTableNumbers = string.Join("و", tables.OrderBy(t => t.TableNumber).Select(t => t.TableNumber));

                // Create a simple response object to avoid circular reference
                var responseData = new
                {
                    Id = primaryOrder.Id,
                    OrderCode = primaryOrder.OrderCode,
                    OrderType = primaryOrder.OrderType,
                    PaymentMethod = primaryOrder.PaymentMethod,
                    OrderStatus = primaryOrder.OrderStatus,
                    PaymentStatus = primaryOrder.PaymentStatus,
                    InsertDate = primaryOrder.InsertDate,
                    TableId = primaryOrder.TableId,
                    MergedTableNumbers = mergedTableNumbers
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = responseData,
                    ErrorStatus = false,
                    Message = $"تم دمج الطاولات ({mergedTableNumbers}) بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging tables");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء دمج الطاولات"
                });
            }
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpPut("CloseTableOrder")]
        public async Task<ActionResult<GlobalResponse<object>>> CloseTableOrder([FromQuery] int? tableId, [FromBody] List<int>? tableIds = null)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                // Determine which tables to close
                var tablesToClose = new List<int>();
                if (tableIds != null && tableIds.Any())
                {
                    // Multiple tables (merged tables) - sent in body
                    tablesToClose = tableIds.Distinct().ToList();
                }
                else if (tableId.HasValue)
                {
                    // Single table (backward compatibility) - sent as query parameter
                    tablesToClose = new List<int> { tableId.Value };
                }
                else
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "يرجى تحديد طاولة واحدة على الأقل"
                    });
                }

                var closedTables = new List<object>();
                var orderId = (int?)null;

                // Process each table
                foreach (var tid in tablesToClose)
                {
                    var table = await _dbConfig.Tables
                        .FirstOrDefaultAsync(t => t.Id == tid && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                    if (table == null)
                    {
                        continue; // Skip invalid tables
                    }

                    // Get the order associated with this table
                    var orderTable = await _dbConfig.OrderTables
                        .Include(ot => ot.Order)
                        .FirstOrDefaultAsync(ot => ot.TableId == tid && !ot.IsDeleted);

                    if (orderTable != null && orderTable.Order != null)
                    {
                        orderId = orderTable.Order.Id;
                        
                        // Mark OrderTable as deleted
                        orderTable.IsDeleted = true;
                        _dbConfig.OrderTables.Update(orderTable);
                }

                // Update table status to Available
                table.Status = "Available";
                table.CurrentOrderId = null;
                _dbConfig.Tables.Update(table);

                    closedTables.Add(new { TableId = table.Id, TableNumber = table.TableNumber });
                }

                await _dbConfig.SaveChangesAsync();

                // If order exists and all tables are closed, mark order as completed
                if (orderId.HasValue)
                {
                    var remainingOrderTables = await _dbConfig.OrderTables
                        .CountAsync(ot => ot.OrderId == orderId.Value && !ot.IsDeleted);

                    if (remainingOrderTables == 0)
                    {
                        var order = await _dbConfig.CustomerOrders
                            .FirstOrDefaultAsync(o => o.Id == orderId.Value);
                        
                        if (order != null)
                        {
                            order.OrderStatus = "Completed";
                            order.PaymentStatus = "Paid";
                            _dbConfig.CustomerOrders.Update(order);
                            await _dbConfig.SaveChangesAsync();
                        }
                    }
                }

                // Send SignalR notifications for all closed tables
                foreach (var tid in tablesToClose)
                {
                    try
                    {
                        var table = await _dbConfig.Tables
                            .FirstOrDefaultAsync(t => t.Id == tid && !t.IsDeleted && t.InsertByUserId == commercialUserId);
                        
                        if (table != null)
                {
                    await _hubContext.Clients.All.SendAsync("TableUpdated", new
                    {
                        TableId = table.Id,
                        Status = table.Status,
                        TableNumber = table.TableNumber,
                                Zone = table.Zone,
                                CurrentOrderId = (int?)null
                    });
                        }
                }
                catch (Exception ex)
                {
                        _logger.LogError(ex, "Error sending SignalR notification for TableUpdated: TableId={TableId}", tid);
                    }
                }

                var message = tablesToClose.Count > 1 
                    ? $"تم إغلاق حساب {tablesToClose.Count} طاولات بنجاح"
                    : "تم إغلاق حساب الطاولة بنجاح";

                return Ok(new GlobalResponse<object>
                {
                    Data = new { ClosedTables = closedTables, Count = closedTables.Count },
                    ErrorStatus = false,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing table order");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء إغلاق حساب الطاولة"
                });
            }
        }

        [Authorize(Roles = "Commercial,POS,Waiter")]
        [HttpPut("TransferTable")]
        public async Task<ActionResult<GlobalResponse<object>>> TransferTable(int fromTableId, int toTableId)
        {
            try
            {
                var commercialUserId = GetCommercialUserId();
                
                // Get source table
                var fromTable = await _dbConfig.Tables
                    .Include(t => t.CurrentOrder)
                    .FirstOrDefaultAsync(t => t.Id == fromTableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                if (fromTable == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة المصدر غير موجودة"
                    });
                }

                // Check if source table has an active order
                if (fromTable.CurrentOrderId == null || fromTable.CurrentOrder == null)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لا يوجد طلب نشط على هذه الطاولة"
                    });
                }

                // Get destination table
                var toTable = await _dbConfig.Tables
                    .FirstOrDefaultAsync(t => t.Id == toTableId && !t.IsDeleted && t.InsertByUserId == commercialUserId);

                if (toTable == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة الهدف غير موجودة"
                    });
                }

                // Check if destination table is available
                if (toTable.Status != "Available" && toTable.Id != fromTableId)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "الطاولة الهدف غير متاحة"
                    });
                }

                // Get the order
                var order = fromTable.CurrentOrder;
                
                // Update order table ID
                order.TableId = toTableId;
                _dbConfig.CustomerOrders.Update(order);

                // Update source table - make it available
                fromTable.Status = "Available";
                fromTable.CurrentOrderId = null;
                _dbConfig.Tables.Update(fromTable);

                // Update destination table - make it occupied
                toTable.Status = "Occupied";
                toTable.CurrentOrderId = order.Id;
                _dbConfig.Tables.Update(toTable);

                await _dbConfig.SaveChangesAsync();

                // Send SignalR notifications for table updates
                try
                {
                    // Notify about source table update
                    await _hubContext.Clients.All.SendAsync("TableUpdated", new
                    {
                        TableId = fromTable.Id,
                        Status = fromTable.Status,
                        TableNumber = fromTable.TableNumber,
                        Zone = fromTable.Zone,
                        CurrentOrderId = (int?)null
                    });

                    // Notify about destination table update
                    await _hubContext.Clients.All.SendAsync("TableUpdated", new
                    {
                        TableId = toTable.Id,
                        Status = toTable.Status,
                        TableNumber = toTable.TableNumber,
                        Zone = toTable.Zone,
                        CurrentOrderId = order.Id
                    });

                    // Notify about order transfer
                    await _hubContext.Clients.All.SendAsync("OrderTransferred", new
                    {
                        OrderId = order.Id,
                        OrderCode = order.OrderCode,
                        FromTableId = fromTableId,
                        FromTableNumber = fromTable.TableNumber,
                        ToTableId = toTableId,
                        ToTableNumber = toTable.TableNumber
                    });

                    _logger.LogInformation("SignalR notifications sent for table transfer: OrderId={OrderId}, FromTableId={FromTableId}, ToTableId={ToTableId}", 
                        order.Id, fromTableId, toTableId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SignalR notifications for table transfer");
                }

                return Ok(new GlobalResponse<object>
                {
                    Data = new 
                    { 
                        OrderId = order.Id,
                        OrderCode = order.OrderCode,
                        FromTableId = fromTableId,
                        FromTableNumber = fromTable.TableNumber,
                        ToTableId = toTableId,
                        ToTableNumber = toTable.TableNumber
                    },
                    ErrorStatus = false,
                    Message = $"تم نقل الطلب من طاولة {fromTable.TableNumber} إلى طاولة {toTable.TableNumber} بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring table");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "حدث خطأ أثناء نقل الطاولة"
                });
            }
        }

        [Authorize(Roles = "Commercial")]
        [HttpGet("GetOrders")]
        public ActionResult<GlobalResponse<PagedList<OrderDto>>> GetOrders(int pageNumber, int pageSize, string? info, DateTime? startDate, DateTime? endDate, string? orderType, string? paymentMethod, int? deliveryDriverId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<PagedList<OrderDto>>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;

            var items = _dbConfig.CustomerOrders
                    .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId))
                    .Include(x => x.CustomerOrderItem)
                    .ThenInclude(x => x.Item)
                    .Include(x => x.DeliveryDriver)
                    .Include(x => x.OrderTables)
                    .ThenInclude(ot => ot.Table)
                    .AsQueryable();

           
            

            // Filter by OrderCode
            if (!string.IsNullOrEmpty(info))
            {
                items = items.Where(x => x.OrderCode == info);
            }

            // Filter by Date Range
            if (startDate.HasValue && endDate.HasValue)
            {
                endDate = endDate.Value.AddDays(1); // Include the end date in the search
                items = items.Where(x => x.InsertDate >= startDate && x.InsertDate < endDate);
            }

            if (startDate.HasValue && !endDate.HasValue)
            {
                items = items.Where(x => x.InsertDate.Date == startDate.Value.Date);
            }

            // Filter by OrderType
            if (!string.IsNullOrEmpty(orderType))
            {
                items = items.Where(x => x.OrderType == orderType);
            }

            // Filter by PaymentMethod
            if (!string.IsNullOrEmpty(paymentMethod))
            {
                items = items.Where(x => x.PaymentMethod == paymentMethod);
            }

            // Filter by DeliveryDriverId
            if (deliveryDriverId.HasValue)
            {
                items = items.Where(x => x.DeliveryDriverId == deliveryDriverId.Value);
            }

            var totalItems = items.Count();

            // Map to OrderDto after filtering
            var ordersList = items
                .OrderByDescending(x => x.InsertDate)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(x => {
                    // Get tables for this order
                    var orderTables = x.OrderTables?
                        .Where(ot => !ot.IsDeleted && ot.Table != null)
                        .Select(ot => ot.Table!)
                        .ToList() ?? new List<Table>();
                    
                    // Convert to TableDto
                    var tableDtos = orderTables.Select(t => new TableDto
                    {
                        Id = t.Id,
                        TableNumber = t.TableNumber,
                        Capacity = t.Capacity,
                        Status = t.Status,
                        Zone = t.Zone,
                        Notes = t.Notes
                    }).ToList();
                    
                    // Build merged table numbers string (e.g., "1و3و5")
                    var mergedTableNumbers = orderTables.Count > 1
                        ? string.Join("و", orderTables.OrderBy(t => t.TableNumber).Select(t => t.TableNumber))
                        : (orderTables.Count == 1 ? orderTables[0].TableNumber : null);
                    
                    return new OrderDto
                {
                    CustomerOrderItem = x.CustomerOrderItem,
                    OrderPrice = x.CustomerOrderItem != null ? x.CustomerOrderItem.Sum(item => item.SellingPrice * item.Quantity) : 0,
                    OrderCode = x.OrderCode,
                    Id = x.Id,
                    ItemsCount = x.CustomerOrderItem != null ? x.CustomerOrderItem.Count() : 0,
                    DailySequenceNumber = x.DailySequenceNumber,
                    InsertDate = x.InsertDate,
                    CreatedAt = x.InsertDate,
                    CreatedByUserId = x.User != null ? x.User.Id : null,
                    CreatedByUsername = x.User != null ? x.User.Username : null,
                    PaymentMethod = x.PaymentMethod,
                    OrderType = x.OrderType,
                    OrderStatus = x.OrderStatus,
                    Notes = x.Notes,
                    Total = x.CustomerOrderItem != null ? x.CustomerOrderItem.Sum(item => item.SellingPrice * item.Quantity) : 0,
                    DiscountType = x.DiscountType,
                    DiscountValue = x.DiscountValue,
                    DiscountAmount = x.DiscountAmount,
                    DiscountPercent = x.DiscountPercent,
                    OrderSubTotal = x.OrderSubTotal,
                    OrderTotalAfterDiscount = x.OrderTotalAfterDiscount,
                        // Tables information
                        Tables = tableDtos.Any() ? tableDtos : null,
                        MergedTableNumbers = mergedTableNumbers,
                    // Delivery fields
                    DeliveryDriverId = x.DeliveryDriverId,
                    DeliveryDriver = x.DeliveryDriver,
                    DeliveryStatus = x.DeliveryStatus,
                    DeliveryAddress = x.DeliveryAddress,
                    DeliveryPhoneNumber = x.DeliveryPhoneNumber,
                    DeliveryCustomerName = x.DeliveryCustomerName,
                    DeliveryFee = x.DeliveryFee
                    };
                })
                .ToList();

            var pagedResult = new PagedList<OrderDto>(ordersList, totalItems, pageNumber, pageSize);

            var response = new GlobalResponse<PagedList<OrderDto>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;

     
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("ExportOrders")]
        public ActionResult ExportOrders(string? info, DateTime? startDate, DateTime? endDate, string? orderType, string? paymentMethod, int? deliveryDriverId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
                return BadRequest();

            var userInsertByUserId = user.InsertByUserId;
            var items = _dbConfig.CustomerOrders
                .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId))
                .Include(x => x.CustomerOrderItem)
                .Include(x => x.OrderTables)
                .ThenInclude(ot => ot.Table)
                .AsQueryable();

            if (!string.IsNullOrEmpty(info))
                items = items.Where(x => x.OrderCode == info);
            if (startDate.HasValue && endDate.HasValue)
            {
                endDate = endDate.Value.AddDays(1);
                items = items.Where(x => x.InsertDate >= startDate && x.InsertDate < endDate);
            }
            if (startDate.HasValue && !endDate.HasValue)
                items = items.Where(x => x.InsertDate.Date == startDate.Value.Date);
            if (!string.IsNullOrEmpty(orderType))
                items = items.Where(x => x.OrderType == orderType);
            if (!string.IsNullOrEmpty(paymentMethod))
                items = items.Where(x => x.PaymentMethod == paymentMethod);
            if (deliveryDriverId.HasValue)
                items = items.Where(x => x.DeliveryDriverId == deliveryDriverId.Value);

            var ordersList = items
                .OrderByDescending(x => x.InsertDate)
                .ToList()
                .Select(x => new
                {
                    OrderCode = x.OrderCode ?? "",
                    InsertDate = x.InsertDate,
                    OrderType = x.OrderType ?? "",
                    PaymentMethod = x.PaymentMethod ?? "",
                    OrderPrice = x.CustomerOrderItem != null ? x.CustomerOrderItem.Sum(item => item.SellingPrice * item.Quantity) : 0,
                    DiscountAmount = x.DiscountAmount ?? 0,
                    OrderTotalAfterDiscount = x.OrderTotalAfterDiscount,
                    ItemsCount = x.CustomerOrderItem != null ? x.CustomerOrderItem.Count() : 0
                })
                .ToList();

            var csv = new StringBuilder();
            var header = "OrderCode,InsertDate,OrderType,PaymentMethod,OrderPrice,DiscountAmount,FinalTotal,ItemsCount";
            csv.AppendLine(header);
            foreach (var o in ordersList)
            {
                var dateStr = o.InsertDate.ToString("yyyy-MM-dd HH:mm");
                var finalTotal = o.OrderTotalAfterDiscount ?? o.OrderPrice;
                var line = $"\"{EscapeCsv(o.OrderCode)}\",\"{dateStr}\",\"{EscapeCsv(o.OrderType)}\",\"{EscapeCsv(o.PaymentMethod)}\",{o.OrderPrice},{o.DiscountAmount},{finalTotal},{o.ItemsCount}";
                csv.AppendLine(line);
            }

            var csvContent = csv.ToString();
            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(csvContent);
            var bytes = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
            var fileName = $"orders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }

        private string RandomCode()
        {
            // Generate 9-digit order code: timestamp-based to ensure uniqueness
            var random = new Random();
            var timestamp = DateTime.UtcNow.Ticks % 1000000000; // Last 9 digits of ticks
            var randomPart = random.Next(100000, 999999); // 6 digits
            var code = (timestamp + randomPart) % 1000000000; // Ensure 9 digits
            return code.ToString().PadLeft(9, '0'); // Ensure exactly 9 digits
        }

        // get selse count

        [Authorize(Roles = "Commercial")]
        [HttpGet("GetSellsCount")]
        public ActionResult<GlobalResponse<object>> GetSellsCount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            var today = DateTime.Today;

            var customerOrdersQuery = _dbConfig.CustomerOrders
                .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId ||  x.User.InsertByUserId == userId));

            var orderItemsQuery = _dbConfig.CustomerOrderItems
                .Where(x => x.CustomerOrder!.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));


          

            var totalItems = customerOrdersQuery.Count();

            var newOrderCount = new
            {
                total = totalItems,
                thisDay = customerOrdersQuery.Count(x => x.InsertDate.Date == today),
                thisWeek = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-7)),
                thisMonth = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-30))
            };

            var newItemsOrderCount = new
            {
                total = orderItemsQuery.Count(),
                thisDay = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date == today ? x.Quantity : 0),
                thisWeek = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-7) ? x.Quantity : 0),
                thisMonth = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-30) ? x.Quantity : 0)
            };

            var newCount = new
            {
                newOrderCount,
                newItemsOrderCount
            };

            var response = new GlobalResponse<object>
            {
                Data = newCount,
                ErrorStatus = true,
                Message = "Success"
            };

            return response;
        }

        [Authorize(Roles = "Commercial")]
        [HttpGet("GetSellsCountByUser")]
        public ActionResult<GlobalResponse<object>> GetSellsCountByUser()
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var users = _dbConfig.Users.Where(x => x.Id == userId).ToList();

            var finelList = new List<object>();
            foreach( var user in users)
            {
                var today = DateTime.Today;

                var customerOrdersQuery = _dbConfig.CustomerOrders
                    .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                var orderItemsQuery = _dbConfig.CustomerOrderItems
                    .Where(x => x.CustomerOrder!.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                var totalItems = customerOrdersQuery.Count();

                var newOrderCount = new
                {
                    total = totalItems,
                    thisDay = customerOrdersQuery.Count(x => x.InsertDate.Date == today),
                    thisWeek = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-7)),
                    thisMonth = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-30))
                };

                var newItemsOrderCount = new
                {
                    total = orderItemsQuery.Count(),
                    thisDay = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date == today ? x.Quantity : 0),
                    thisWeek = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-7) ? x.Quantity : 0),
                    thisMonth = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-30) ? x.Quantity : 0)
                };

                var newCount = new
                {
                    name = user.Name,
                    newOrderCount,
                    newItemsOrderCount
                };

                finelList.Add(newOrderCount);
              
            }   
            
            var response = new GlobalResponse<object>
            {
                Data = finelList,
                ErrorStatus = true,
                Message = "Success"
            };

            return response;
        }


        [Authorize(Roles = "Commercial,Admin,POS")]
        [HttpGet("GetDashboardStats")]
        public ActionResult<GlobalResponse<object>> GetDashboardStats()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                var today = DateTime.Today;

                // Orders Statistics
                var customerOrdersQuery = _dbConfig.CustomerOrders
                    .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.InsertByUserId == userId));

                var orderItemsQuery = _dbConfig.CustomerOrderItems
                    .Where(x => x.CustomerOrder!.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                // Items Statistics
                var itemsQuery = _dbConfig.Items
                    .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                // Users Statistics
                var usersQuery = _dbConfig.Users
                    .Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.Id == user.InsertByUserId || x.InsertByUserId == userId));

                // Categories Statistics
                var tagsQuery = _dbConfig.Tags
                    .Where(x => x.IsDeleted == false);

                // Sales Amount
                decimal CalculateSalesAmount(DateTime startDate, DateTime endDate)
                {
                    return orderItemsQuery
                        .Where(x => x.CustomerOrder != null &&
                                    x.CustomerOrder.InsertDate.Date >= startDate &&
                                    x.CustomerOrder.InsertDate.Date <= endDate)
                        .Sum(x => x.Quantity * x.SellingPrice);
                }

                decimal TotalAmount()
                {
                    return orderItemsQuery.Sum(x => x.Quantity * x.SellingPrice);
                }

                var stats = new
                {
                    orders = new
                    {
                        total = customerOrdersQuery.Count(),
                        today = customerOrdersQuery.Count(x => x.InsertDate.Date == today),
                        thisWeek = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-7)),
                        thisMonth = customerOrdersQuery.Count(x => x.InsertDate.Date >= today.AddDays(-30))
                    },
                    items = new
                    {
                        total = orderItemsQuery.Count(),
                        today = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date == today ? x.Quantity : 0),
                        thisWeek = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-7) ? x.Quantity : 0),
                        thisMonth = orderItemsQuery.Sum(x => x.CustomerOrder!.InsertDate.Date >= today.AddDays(-30) ? x.Quantity : 0)
                    },
                    salesAmount = new
                    {
                        total = TotalAmount(),
                        today = CalculateSalesAmount(today, today),
                        thisWeek = CalculateSalesAmount(today.AddDays(-7), today),
                        thisMonth = CalculateSalesAmount(today.AddDays(-30), today)
                    },
                    products = new
                    {
                        total = itemsQuery.Count(),
                        active = itemsQuery.Count(x => x.IsDeleted == false)
                    },
                    users = new
                    {
                        total = usersQuery.Count(),
                        active = usersQuery.Count(x => x.IsDeleted == false)
                    },
                    categories = new
                    {
                        total = tagsQuery.Count(),
                        active = tagsQuery.Count(x => x.IsDeleted == false)
                    }
                };

                var response = new GlobalResponse<object>
                {
                    Data = stats,
                    ErrorStatus = false,
                    Message = "Success"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        // Advanced Reports Endpoints

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetProfitReport")]
        public ActionResult<GlobalResponse<object>> GetProfitReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                var orderItemsQuery = _dbConfig.CustomerOrderItems
                    .Include(x => x.Item)
                    .Include(x => x.CustomerOrder)
                    .Where(x => x.CustomerOrder!.IsDeleted == false && 
                                (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                if (startDate.HasValue)
                {
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.InsertDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    endDate = endDate.Value.AddDays(1);
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.InsertDate.Date < endDate.Value.Date);
                }

                var profitData = orderItemsQuery
                    .Select(x => new
                    {
                        SellingPrice = x.SellingPrice,
                        PurchasingPrice = x.Item.PurchasingPrice,
                        Quantity = x.Quantity
                    })
                    .ToList();

                var totalSales = profitData.Sum(x => x.SellingPrice * x.Quantity);
                var totalCost = profitData.Sum(x => x.PurchasingPrice * x.Quantity);
                var totalProfit = totalSales - totalCost;
                var profitMargin = totalSales > 0 ? (totalProfit / totalSales) * 100 : 0;

                var report = new
                {
                    totalSales = totalSales,
                    totalCost = totalCost,
                    totalProfit = totalProfit,
                    profitMargin = Math.Round(profitMargin, 2),
                    totalItemsSold = profitData.Sum(x => x.Quantity),
                    period = new
                    {
                        startDate = startDate?.ToString("yyyy-MM-dd"),
                        endDate = endDate?.AddDays(-1).ToString("yyyy-MM-dd")
                    }
                };

                return Ok(new GlobalResponse<object>
                {
                    Data = report,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profit report");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetTopSellingItems")]
        public ActionResult<GlobalResponse<object>> GetTopSellingItems(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                var orderItemsQuery = _dbConfig.CustomerOrderItems
                    .Include(x => x.Item)
                    .Include(x => x.CustomerOrder)
                    .Where(x => x.CustomerOrder!.IsDeleted == false && 
                                (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                if (startDate.HasValue)
                {
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.InsertDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    endDate = endDate.Value.AddDays(1);
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.InsertDate.Date < endDate.Value.Date);
                }

                var topItems = orderItemsQuery
                    .GroupBy(x => new { x.ItemId, x.Item.Name, x.Item.Code })
                    .Select(g => new
                    {
                        itemId = g.Key.ItemId,
                        itemName = g.Key.Name,
                        itemCode = g.Key.Code,
                        totalQuantitySold = g.Sum(x => x.Quantity),
                        totalSales = g.Sum(x => x.SellingPrice * x.Quantity),
                        orderCount = g.Select(x => x.CustomerOrderId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.totalQuantitySold)
                    .Take(topCount)
                    .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = topItems,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top selling items");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetSalesByCategory")]
        public ActionResult<GlobalResponse<object>> GetSalesByCategory(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);
                var commercialUserId = GetCommercialUserId();

                var orderItemsQuery = _dbConfig.CustomerOrderItems
                    .Include(x => x.Item)
                    .Include(x => x.CustomerOrder)
                    .Where(x => x.CustomerOrder!.IsDeleted == false && 
                                (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                if (startDate.HasValue)
                {
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.InsertDate.Date >= startDate.Value.Date);
                }

                DateTime? endDateForExpenses = endDate;
                if (endDate.HasValue)
                {
                    endDateForExpenses = endDate.Value.AddDays(1);
                    orderItemsQuery = orderItemsQuery.Where(x => x.CustomerOrder!.InsertDate.Date < endDateForExpenses.Value.Date);
                }

                var salesByCategory = orderItemsQuery
                    .Where(x => !string.IsNullOrEmpty(x.Item.Tags))
                    .GroupBy(x => x.Item.Tags)
                    .Select(g => new
                    {
                        category = g.Key,
                        totalSales = g.Sum(x => x.SellingPrice * x.Quantity),
                        totalQuantity = g.Sum(x => x.Quantity),
                        itemCount = g.Select(x => x.ItemId).Distinct().Count(),
                        orderCount = g.Select(x => x.CustomerOrderId).Distinct().Count()
                    })
                    .ToList();

                // التاجات ضمن نطاق المستخدم (نفس منطق GetTags)
                var userInsertByUserId = user.InsertByUserId;
                var tagsInScope = _dbConfig.Tags
                    .Include(x => x.User)
                    .Where(x => !x.IsDeleted && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId))
                    .ToList();
                var tagNameToId = tagsInScope
                    .Where(t => !string.IsNullOrEmpty(t.Name))
                    .GroupBy(t => t.Name!)
                    .ToDictionary(g => g.Key, g => g.First().Id);

                // صرفيات الفئة حسب TagId فقط (نفس الفترة)
                var expensesQuery = _dbConfig.Expenses
                    .Where(e => !e.IsDeleted && e.InsertByUserId == commercialUserId && e.TagId != null);

                if (startDate.HasValue)
                    expensesQuery = expensesQuery.Where(e => e.Date.Date >= startDate.Value.Date);
                if (endDateForExpenses.HasValue)
                    expensesQuery = expensesQuery.Where(e => e.Date.Date < endDateForExpenses.Value.Date);

                var expensesByTagIdList = expensesQuery
                    .GroupBy(e => e.TagId!.Value)
                    .Select(g => new { TagId = g.Key, TotalAmount = g.Sum(e => e.Amount) })
                    .ToList();
                var expensesByTagIdDict = expensesByTagIdList.ToDictionary(x => x.TagId, x => x.TotalAmount);

                // أسماء الفئات: مبيعات + تاجات ظهرت في الصرفيات عبر TagId
                var tagIdsInExpenses = expensesByTagIdList.Select(x => x.TagId).Distinct().ToList();
                var tagNamesFromExpenses = tagsInScope
                    .Where(t => tagIdsInExpenses.Contains(t.Id) && !string.IsNullOrEmpty(t.Name))
                    .Select(t => t.Name!);
                var salesCategoryNames = salesByCategory.Select(s => s.category).Where(c => !string.IsNullOrEmpty(c));
                var allCategoryNames = salesCategoryNames.Union(tagNamesFromExpenses).Distinct().ToList();

                var merged = allCategoryNames.Select(cat => new
                {
                    category = cat,
                    totalSales = salesByCategory.FirstOrDefault(s => s.category == cat)?.totalSales ?? 0,
                    totalQuantity = salesByCategory.FirstOrDefault(s => s.category == cat)?.totalQuantity ?? 0,
                    itemCount = salesByCategory.FirstOrDefault(s => s.category == cat)?.itemCount ?? 0,
                    orderCount = salesByCategory.FirstOrDefault(s => s.category == cat)?.orderCount ?? 0,
                    totalExpenses = tagNameToId.TryGetValue(cat, out var tagId) && expensesByTagIdDict.TryGetValue(tagId, out var amt) ? amt : 0
                })
                .OrderByDescending(x => x.totalSales)
                .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = merged,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales by category");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetSalesByEmployee")]
        public ActionResult<GlobalResponse<object>> GetSalesByEmployee(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                var ordersQuery = _dbConfig.CustomerOrders
                    .Include(x => x.User)
                    .Include(x => x.CustomerOrderItem)
                    .Where(x => x.IsDeleted == false && 
                                (x.InsertByUserId == userId || x.User.InsertByUserId == userId));

                if (startDate.HasValue)
                {
                    ordersQuery = ordersQuery.Where(x => x.InsertDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    endDate = endDate.Value.AddDays(1);
                    ordersQuery = ordersQuery.Where(x => x.InsertDate.Date < endDate.Value.Date);
                }

                var salesByEmployee = ordersQuery
                    .GroupBy(x => new { x.InsertByUserId, x.User.Username })
                    .Select(g => new
                    {
                        employeeId = g.Key.InsertByUserId,
                        employeeName = g.Key.Username,
                        totalOrders = g.Count(),
                        totalSales = g.SelectMany(o => o.CustomerOrderItem).Sum(x => x.SellingPrice * x.Quantity),
                        totalItemsSold = g.SelectMany(o => o.CustomerOrderItem).Sum(x => x.Quantity)
                    })
                    .OrderByDescending(x => x.totalSales)
                    .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = salesByEmployee,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales by employee");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"??? ???: {ex.Message}"
                });
            }
        }

        // get Item Price 
        [Authorize(Roles = "Commercial,POS,Reader")]
        [HttpDelete("ItemPrice")]
        public async Task<ActionResult<GlobalResponse<int>>> ItemPrice(string code)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.Where(x => x.InsertByUserId == userId).FirstOrDefault();

            var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Code == code && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
            if (item == null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "item not exsit"
                });
            }
            return Ok(new GlobalResponse<Item>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }


        // POST: api/Admin/UploadItemImage/{itemId}
        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("UploadItemImage/{itemId}")]
        public async Task<ActionResult<GlobalResponse<object>>> UploadItemImage(int itemId, [FromForm] IFormFile image)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                if (user == null)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Id == itemId && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                if (item == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Item not found"
                    });
                }

                if (image == null || image.Length == 0)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Image file is required"
                    });
                }

                var imageFileName = await UploadIamgesAsync(image);
                item.Image = imageFileName;
                _dbConfig.Items.Update(item);
                await _dbConfig.SaveChangesAsync();

                var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";
                var fullImageUrl = imageBaseUrl + imageFileName;

                return Ok(new GlobalResponse<object>
                {
                    Data = new { Image = fullImageUrl, ImageFileName = imageFileName },
                    ErrorStatus = false,
                    Message = "تم رفع الصورة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading item image for item {ItemId}", itemId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء رفع الصورة: {ex.Message}"
                });
            }
        }

        // POST: api/Admin/UploadMultipleItemImages
        [Authorize(Roles = "Commercial,POS")]
        [HttpPost("UploadMultipleItemImages")]
        public async Task<ActionResult<GlobalResponse<object>>> UploadMultipleItemImages([FromForm] List<IFormFile> images, [FromForm] List<int> itemIds)
        {
            try
            {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                if (user == null)
            {
                    return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                if (images == null || !images.Any())
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Images are required"
                    });
                }

                if (itemIds == null || !itemIds.Any())
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Item IDs are required"
                    });
                }

                if (images.Count != itemIds.Count)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Number of images must match number of item IDs"
                    });
                }

                var results = new List<object>();
                var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";

                for (int i = 0; i < images.Count; i++)
                {
                    var image = images[i];
                    var itemId = itemIds[i];

                    try
                    {
                        var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Id == itemId && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));
                        
                        if (item == null)
                        {
                            results.Add(new { ItemId = itemId, Success = false, Message = "Item not found" });
                            continue;
                        }

                        if (image == null || image.Length == 0)
                        {
                            results.Add(new { ItemId = itemId, Success = false, Message = "Image file is empty" });
                            continue;
                        }

                        var imageFileName = await UploadIamgesAsync(image);
                        item.Image = imageFileName;
                        _dbConfig.Items.Update(item);
                        
                        results.Add(new 
                        { 
                            ItemId = itemId, 
                            ItemName = item.Name,
                            Success = true, 
                            Image = imageBaseUrl + imageFileName,
                            ImageFileName = imageFileName,
                            Message = "تم رفع الصورة بنجاح" 
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading image for item {ItemId}", itemId);
                        results.Add(new { ItemId = itemId, Success = false, Message = $"حدث خطأ: {ex.Message}" });
                    }
                }

                await _dbConfig.SaveChangesAsync();

                var successCount = results.Count(r => ((dynamic)r).Success == true);
                var failCount = results.Count - successCount;

                return Ok(new GlobalResponse<object>
            {
                    Data = new 
                    { 
                        Results = results,
                        SuccessCount = successCount,
                        FailCount = failCount,
                        TotalCount = results.Count
                    },
                ErrorStatus = false,
                    Message = $"تم رفع {successCount} صورة بنجاح من أصل {results.Count}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading multiple item images");
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء رفع الصور: {ex.Message}"
            });
        }
        }

        [Authorize(Roles = "Admin,Commercial")]
        [HttpPost("GenerateItemImageWithAI")]
        public async Task<ActionResult<GlobalResponse<string>>> GenerateItemImageWithAI(GenerateItemImageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ItemName))
                {
                    return BadRequest(new GlobalResponse<string>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "اسم الطبق مطلوب"
                    });
                }

                var apiKey = _configuration["OpenAISettings:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new GlobalResponse<string>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "OpenAI API Key غير موجود في الإعدادات"
                    });
                }

                // بناء prompt للصورة
                var prompt = $"A high-quality, appetizing food photography image of {request.ItemName}";
                if (!string.IsNullOrWhiteSpace(request.Category))
                {
                    prompt += $" from the {request.Category} category";
                }
                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    prompt += $". {request.Description}";
                }
                prompt += ". Professional food photography, well-lit, appetizing, restaurant quality, on a clean plate or dish, high resolution, realistic style.";

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = "dall-e-3",
                    prompt = prompt,
                    n = 1,
                    size = "1024x1024",
                    quality = "standard"
                };

                var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/images/generations", requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI DALL-E API Error: {Error}", errorContent);
                    return StatusCode(500, new GlobalResponse<string>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "حدث خطأ أثناء الاتصال بـ OpenAI API"
                    });
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var imageUrl = jsonResponse.GetProperty("data")[0].GetProperty("url").GetString();

                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    return BadRequest(new GlobalResponse<string>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "لم يتم الحصول على صورة من OpenAI"
                    });
                }

                return Ok(new GlobalResponse<string>
                {
                    Data = imageUrl,
                    ErrorStatus = false,
                    Message = "تم إنشاء الصورة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating item image with AI");
                return StatusCode(500, new GlobalResponse<string>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Admin,Commercial")]
        [HttpPost("SaveGeneratedItemImage/{itemId}")]
        public async Task<ActionResult<GlobalResponse<object>>> SaveGeneratedItemImage(int itemId, [FromBody] SaveGeneratedImageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ImageUrl))
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "رابط الصورة مطلوب"
                    });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                if (user == null)
                {
                    return BadRequest(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "User not found"
                    });
                }

                var item = await _dbConfig.Items.FirstOrDefaultAsync(x => x.Id == itemId && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                if (item == null)
                {
                    return NotFound(new GlobalResponse<object>
                    {
                        Data = null,
                        ErrorStatus = true,
                        Message = "Item not found"
                    });
                }

                // تحميل الصورة من URL وحفظها
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(request.ImageUrl);

                var imageExtension = ".png"; // DALL-E يعيد PNG
                var fileName = $"{Guid.NewGuid()}{imageExtension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var filePath = Path.Combine(path, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                item.Image = fileName;
                _dbConfig.Items.Update(item);
                await _dbConfig.SaveChangesAsync();

                var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";
                var fullImageUrl = imageBaseUrl + fileName;

                return Ok(new GlobalResponse<object>
                {
                    Data = new { Image = fullImageUrl, ImageFileName = fileName },
                    ErrorStatus = false,
                    Message = "تم حفظ الصورة بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving generated item image for item {ItemId}", itemId);
                return StatusCode(500, new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ أثناء حفظ الصورة: {ex.Message}"
                });
            }
        }
        

        // upload images 
        private async Task<string> UploadIamgesAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                throw new ArgumentException("Image file is null or empty");
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var validImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            
            var fileName = imageFile.FileName;
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Image file name is null or empty");
            }

            var fileExtension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(fileExtension) || !validImageExtensions.Contains(fileExtension.ToLower()))
            {
                throw new ArgumentException("Invalid image extension. Allowed extensions: .jpg, .jpeg, .png, .gif");
            }

            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(path, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return uniqueFileName;
        }

        // Seed Database
   //     [Authorize(Roles = "Admin")]
        [HttpPost("SeedData")]
        public ActionResult<GlobalResponse<string>> ExecuteSeedData([FromBody] SeedDataRequest request)
        {
            try
            {
                int commercialUserId = request.CommercialUserId;
                RestaurantPOS.Db.SeedData.SeedDatabase(_dbConfig, commercialUserId);

                var message =
                    $"تم إضافة البيانات بنجاح للمستخدم التجاري رقم {commercialUserId}";
                
                return Ok(new GlobalResponse<string>
                {
                    Data = "Database seeded successfully",
                    ErrorStatus = false,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                return BadRequest(new GlobalResponse<string>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"خطأ في إضافة البيانات: {ex.Message}"
                });
            }
        }

    }

}
