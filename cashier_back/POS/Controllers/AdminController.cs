using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Db;
using POS.Models;
using POS.Models.Dtos;
using POS.Models.Requests;
using POS.Models.Response;
using System.Security.Claims;

namespace POS.Controllers
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

        public AdminController(ILogger<AdminController> logger, DbConfig dbConfig, IMapper mapper, IConfiguration configuration)
        {
            _logger = logger;
            _dbConfig = dbConfig;
            _mapper = mapper;
            _configuration = configuration;
        }

        // Add User
        [Authorize(Roles = "Commercial,Admin")]
        [HttpPost("AddUser")]
        public async Task<ActionResult<GlobalResponse<User>>> AddUser(UserRequest request)
        {
            var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber && x.IsDeleted == false);
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            if (user != null)
            {
                return BadRequest(new GlobalResponse<User>
                {
                    Data = user,
                    ErrorStatus = true,
                    Message = "phone number is already exsit"
                });
            }
            var newUse = _mapper.Map<User>(request);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            newUse.Password = passwordHash;
            newUse.InsertByUserId = userId;
            _dbConfig.Users.Add(newUse);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<User>
            {
                Data = newUse,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpPut("UpdateUser")]
        public async Task<ActionResult<GlobalResponse<User>>> UpdateUser(UserRequest request, int id)
        {
            var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
            if (user == null)
            {
                return BadRequest(new GlobalResponse<User>
                {
                    Data = user,
                    ErrorStatus = true,
                    Message = "user not exsit"
                });
            }
            var uUser = _mapper.Map(request, user);
            var passwordHash = request.Password == null ? user!.Password : BCrypt.Net.BCrypt.HashPassword(request.Password);
            uUser!.Password = passwordHash;
            _dbConfig.Users.Update(uUser);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<User>
            {
                Data = user,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpDelete("DeleteUser")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteUser(int id)
        {
            var user = await _dbConfig.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
            if (user == null)
            {
                return BadRequest(new GlobalResponse<User>
                {
                    Data = user,
                    ErrorStatus = true,
                    Message = "user not exsit"
                });
            }

            user!.IsDeleted = true;
            _dbConfig.Users.Update(user);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<User>
            {
                Data = user,
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

            if (userInfo.Role == "Admin")
            {
                var user = _dbConfig.Users.Where(x => x.IsDeleted == false ).AsQueryable();

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
                var user = _dbConfig.Users.Where(x => x.IsDeleted == false && x.InsertByUserId == userId).AsQueryable();

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

            var tag = await _dbConfig.Tags.FirstOrDefaultAsync(x => x.Name == request.Name && x.IsDeleted == false && x.InsertByUserId == userId);
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

            var userInsertByUserId = user.InsertByUserId;
            var tag = await _dbConfig.Tags.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId));
            if (tag == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = tag,
                    ErrorStatus = true,
                    Message = "tag not exsit"
                });
            }
            var uTag = _mapper.Map(request, tag);

            _dbConfig.Tags.Update(uTag);
            await _dbConfig.SaveChangesAsync();

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
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var tag = await _dbConfig.Tags.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId));
            if (tag == null)
            {
                return BadRequest(new GlobalResponse<Tag>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "tag not exsit"
                });
            }

            tag!.IsDeleted = true;
            _dbConfig.Tags.Update(tag);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<Tag>
            {
                Data = tag,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial,POS")]
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
           

            item.Tags = request.Tags;
            item.PurchasingPrice = request.PurchasingPrice;
            item.DisCountPrice = request.DisCountPrice;
            item.Description = request.Description;
            item.SellingPrice = request.SellingPrice;
            item.Quantity = request.Quantity;
            item.Code = request.Code;
            item.Name = request.Name;
            item.Image = request.Image != null ? await UploadIamgesAsync(request.Image): item.Image;


            _dbConfig.Items.Update(item);
            await _dbConfig.SaveChangesAsync();

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

            return Ok(new GlobalResponse<Item>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }


        [Authorize(Roles = "Commercial,POS")]
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
            var item = _dbConfig.Items.Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId)).AsQueryable();

            if (info != null)
            {
                item = item.Where(x => x.Code == info || x.Name.Contains(info) || x.Description!.Contains(info) || x.Tags!.Contains(info));
            }

            var imageBaseUrl = _configuration["ApiSettings:ImageBaseUrl"] ?? "https://pos-api.tatwer.tech/Images/";

            foreach(var n in item)
            {
                if (!string.IsNullOrEmpty(n.Image))
                {
                    n.Image = imageBaseUrl + n.Image;
                }
            }
            var totalItems = item.Count();


            var pagedResult = new PagedList<Item>(item.ToList(), totalItems, pageNumber, pageSize);

            var response = new GlobalResponse<PagedList<Item>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [Authorize(Roles = "Commercial,POS,Reader")]
        [HttpGet("GetItemsByCode")]
        public async Task<ActionResult<GlobalResponse<Object>>> GetItemsByCode(string code)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                return BadRequest(new GlobalResponse<Object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

            var userInsertByUserId = user.InsertByUserId;
            var item =await _dbConfig.Items.Where(x => x.IsDeleted == false && (x.InsertByUserId == userId || x.User.Id == userInsertByUserId || x.User.InsertByUserId == userId) && x.Code == code).FirstOrDefaultAsync();



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

            if (!string.IsNullOrEmpty(item.Image))
            {
                item.Image = imageBaseUrl + item.Image;
            }
            
            var response = new GlobalResponse<Object>
            {
                Data = item,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;
        }

        [Authorize(Roles = "Commercial,POS")]
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
                
                var items = _dbConfig.Items
                    .Where(x => !x.IsDeleted && (x.InsertByUserId == userId ||  x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId))
                    .ToList();

                var orderCode = request.OrderCode ?? RandomCode();
                var newOrder = new CustomerOrder
                {
                    OrderCode = orderCode,
                    PaymentMethod = request.PaymentMethod ?? "Cash",
                    InsertByUserId = userId,
                    
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

                    // Check inventory availability before processing
                    var itemsToUpdate = new List<Item>();
                    foreach (var itemRequest in request.CustomerOrderItem)
                    {
                        var currentItem = items.FirstOrDefault(x => x.Id == itemRequest.ItemId);
                        if (currentItem == null) continue;

                        // Calculate total quantity needed for this item (including duplicates in order)
                        var totalQuantityNeeded = request.CustomerOrderItem
                            .Where(x => x.ItemId == itemRequest.ItemId)
                            .Sum(x => x.Quantity);

                        // Check if enough quantity is available
                        if (currentItem.Quantity < totalQuantityNeeded)
                        {
                            _logger.LogWarning("Insufficient inventory for item {ItemId}: Available {Available}, Required {Required}", 
                                itemRequest.ItemId, currentItem.Quantity, totalQuantityNeeded);
                            return BadRequest(new GlobalResponse<CustomerOrder>
                            {
                                Data = null,
                                ErrorStatus = true,
                                Message = $"Insufficient inventory for item '{currentItem.Name}'. Available: {currentItem.Quantity}, Required: {totalQuantityNeeded}"
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
                            
                            // Track items to update inventory
                            if (!itemsToUpdate.Any(x => x.Id == currentItem.Id))
                            {
                                itemsToUpdate.Add(currentItem);
                            }
                        }
                    }

                    // Update inventory quantities
                    foreach (var itemToUpdate in itemsToUpdate)
                    {
                        var totalQuantitySold = insertItems
                            .Where(x => x.ItemId == itemToUpdate.Id)
                            .Sum(x => x.Quantity);
                        
                        itemToUpdate.Quantity -= totalQuantitySold;
                        _dbConfig.Items.Update(itemToUpdate);
                    }

                    _dbConfig.CustomerOrderItems.AddRange(insertItems);
                    await _dbConfig.SaveChangesAsync();
                }

                _logger.LogInformation("Order created successfully: {OrderCode} by user {UserId}", orderCode, userId);

                return Ok(new GlobalResponse<CustomerOrder>
                {
                    Data = newOrder,
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
        [HttpDelete("DeleteOrder")]
        public async Task<ActionResult<GlobalResponse<int>>> DeleteOrder(int id)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

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

            item!.IsDeleted = true;
            _dbConfig.CustomerOrders.Update(item);
            await _dbConfig.SaveChangesAsync();

            return Ok(new GlobalResponse<CustomerOrder>
            {
                Data = item,
                ErrorStatus = false,
                Message = "done"
            });
        }

        [Authorize(Roles = "Commercial")]
        [HttpGet("GetOrders")]
        public ActionResult<GlobalResponse<PagedList<OrderDto>>> GetOrders(int pageNumber, int pageSize, string? info, DateTime? startDate, DateTime? endDate)
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
                    .AsQueryable()
                    .Select(x => new OrderDto
                    {
                        CustomerOrderItem = x.CustomerOrderItem,
                        OrderPrice = x.CustomerOrderItem.Sum(item => item.SellingPrice * item.Quantity), // Fixed: Use actual selling price from order item with quantity
                        OrderCode = x.OrderCode,
                        Id = x.Id,
                        ItemsCount = x.CustomerOrderItem.Count(),
                        InsertDate = x.InsertDate
                    });

           
            

            if (info != null)
            {
                items = items.Where(x => x.OrderCode == info);
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                endDate = endDate.Value.AddDays(1); // Include the end date in the search
                items = items.Where(x => x.InsertDate >= startDate && x.InsertDate < endDate);
            }

            if (startDate.HasValue && !endDate.HasValue)
            {
                items = items.Where(x => x.InsertDate.Date == startDate.Value.Date);
            }

            var totalItems = items.Count();

            var pagedResult = new PagedList<OrderDto>(items.ToList(), totalItems, pageNumber, pageSize);

            var response = new GlobalResponse<PagedList<OrderDto>>
            {
                Data = pagedResult,
                ErrorStatus = false,
                Message = "Success"
            };

            return response;

     
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

            if (user == null)
            {
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

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
                ErrorStatus = false,
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
                ErrorStatus = false,
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
                    Message = $"حدث خطأ: {ex.Message}"
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
                    Message = $"حدث خطأ: {ex.Message}"
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
                    Message = $"حدث خطأ: {ex.Message}"
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
                    .OrderByDescending(x => x.totalSales)
                    .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = salesByCategory,
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
                    Message = $"حدث خطأ: {ex.Message}"
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
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        [Authorize(Roles = "Commercial,Admin")]
        [HttpGet("GetLowStockItems")]
        public ActionResult<GlobalResponse<object>> GetLowStockItems(int threshold = 10)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = _dbConfig.Users.FirstOrDefault(x => x.Id == userId);

                var itemsQuery = _dbConfig.Items
                    .Where(x => x.IsDeleted == false && 
                                (x.InsertByUserId == userId || x.User.Id == user.InsertByUserId || x.User.InsertByUserId == userId));

                var lowStockItems = itemsQuery
                    .Where(x => x.Quantity <= threshold)
                    .Select(x => new
                    {
                        itemId = x.Id,
                        itemName = x.Name,
                        itemCode = x.Code,
                        currentQuantity = x.Quantity,
                        threshold = threshold,
                        category = x.Tags
                    })
                    .OrderBy(x => x.currentQuantity)
                    .ToList();

                return Ok(new GlobalResponse<object>
                {
                    Data = lowStockItems,
                    ErrorStatus = false,
                    Message = "Success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock items");
                return BadRequest(new GlobalResponse<object>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        // get Item Price 
        [Authorize(Roles = "Commercial,POS,Reader")]
        [HttpGet("ItemPrice")]
        public async Task<ActionResult<GlobalResponse<Item>>> ItemPrice(string code)
        {

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = _dbConfig.Users.Where(x => x.InsertByUserId == userId).FirstOrDefault();

            if (user == null)
            {
                return BadRequest(new GlobalResponse<Item>
                {
                    Data = null,
                    ErrorStatus = true,
                    Message = "User not found"
                });
            }

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


        // upload images 
        private async Task<string> UploadIamgesAsync(IFormFile imageFile)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var validImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
          

            var fileExtension = Path.GetExtension(imageFile.FileName);
            if (!validImageExtensions.Contains(fileExtension.ToLower()))
            {
                return "not a valid image extension";
            }

            var fileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(path, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }

        // Seed Database
   //     [Authorize(Roles = "Admin")]
        [HttpPost("SeedData")]
        public ActionResult<GlobalResponse<string>> ExecuteSeedData([FromBody] SeedDataRequest request)
        {
            try
            {
                int commercialUserId = request.CommercialUserId;
                POS.Db.SeedData.SeedDatabase(_dbConfig, commercialUserId);

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
                    Message = $"حدث خطأ أثناء إضافة البيانات: {ex.Message}"
                });
            }
        }

    }

}