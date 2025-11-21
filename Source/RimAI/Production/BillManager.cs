using System.Linq;
using RimWorld;
using Verse;

namespace RimAI.Production
{
    /// <summary>
    /// RimWorld Bill 생성/수정/삭제를 담당하는 유틸리티 클래스
    /// </summary>
    public static class BillManager
    {
        /// <summary>
        /// Bill 생성 결과
        /// </summary>
        public class BillResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; } = "";
            public Bill? CreatedBill { get; set; }
        }

        /// <summary>
        /// 새 Bill 생성
        /// </summary>
        public static BillResult CreateBill(
            Building_WorkTable workTable,
            RecipeDef recipe,
            int targetCount = -1,
            QualityCategory minQuality = QualityCategory.Normal,
            ThingDef? stuffDef = null)
        {
            var result = new BillResult();

            // 1. 작업대 검증
            if (workTable == null || workTable.Destroyed)
            {
                result.ErrorMessage = "작업대가 유효하지 않습니다";
                return result;
            }

            // 2. 레시피 검증
            if (recipe == null)
            {
                result.ErrorMessage = "레시피가 null입니다";
                return result;
            }

            // 3. 작업대가 레시피를 지원하는지 확인
            if (!recipe.AllRecipeUsers.Contains(workTable.def))
            {
                result.ErrorMessage = $"{workTable.def.label}는 {recipe.label} 레시피를 지원하지 않습니다";
                return result;
            }

            // 4. 연구 완료 여부 확인
            if (recipe.researchPrerequisite != null && !recipe.researchPrerequisite.IsFinished)
            {
                result.ErrorMessage = $"{recipe.label} 레시피는 아직 연구되지 않았습니다";
                return result;
            }

            // 5. Bill 생성
            Bill_Production bill = new Bill_Production(recipe);

            // 6. 반복 모드 설정
            if (targetCount > 0)
            {
                bill.repeatMode = BillRepeatModeDefOf.TargetCount;
                bill.targetCount = targetCount;
                bill.pauseWhenSatisfied = true;
                bill.unpauseWhenYouHave = targetCount / 2; // 절반 이하로 떨어지면 재개
            }
            else
            {
                bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
                bill.repeatCount = 1;
            }

            // 7. 품질 필터 설정 (의류/무기용)
            if (recipe.products != null && recipe.products.Count > 0)
            {
                var product = recipe.products[0].thingDef;
                if (product.HasComp(typeof(CompQuality)))
                {
                    bill.qualityRange = new QualityRange(minQuality, QualityCategory.Legendary);
                }
            }

            // 8. 재질 필터 설정
            if (stuffDef != null && recipe.products != null && recipe.products.Count > 0)
            {
                var product = recipe.products[0].thingDef;
                if (product.MadeFromStuff)
                {
                    bill.ingredientFilter.SetAllow(stuffDef, true);
                }
            }

            // 9. 기본 설정
            bill.suspended = false;

            // 10. BillStack에 추가
            try
            {
                workTable.billStack.AddBill(bill);
                result.Success = true;
                result.CreatedBill = bill;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"Bill 추가 중 오류: {ex.Message}";
                return result;
            }

            return result;
        }

        /// <summary>
        /// Bill 수정 (목표 수량 변경)
        /// </summary>
        public static BillResult ModifyBill(Bill bill, int newTargetCount)
        {
            var result = new BillResult();

            if (bill == null)
            {
                result.ErrorMessage = "Bill이 null입니다";
                return result;
            }

            if (!(bill is Bill_Production productionBill))
            {
                result.ErrorMessage = "Production Bill이 아닙니다";
                return result;
            }

            try
            {
                productionBill.targetCount = newTargetCount;
                productionBill.unpauseWhenYouHave = newTargetCount / 2;
                result.Success = true;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"Bill 수정 중 오류: {ex.Message}";
                return result;
            }

            return result;
        }

        /// <summary>
        /// Bill 일시 중단/재개
        /// </summary>
        public static BillResult PauseBill(Bill bill, bool pause)
        {
            var result = new BillResult();

            if (bill == null)
            {
                result.ErrorMessage = "Bill이 null입니다";
                return result;
            }

            try
            {
                bill.suspended = pause;
                result.Success = true;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"Bill 중단/재개 중 오류: {ex.Message}";
                return result;
            }

            return result;
        }

        /// <summary>
        /// Bill 삭제
        /// </summary>
        public static BillResult DeleteBill(Building_WorkTable workTable, Bill bill)
        {
            var result = new BillResult();

            if (workTable == null || workTable.Destroyed)
            {
                result.ErrorMessage = "작업대가 유효하지 않습니다";
                return result;
            }

            if (bill == null)
            {
                result.ErrorMessage = "Bill이 null입니다";
                return result;
            }

            try
            {
                workTable.billStack.Delete(bill);
                result.Success = true;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"Bill 삭제 중 오류: {ex.Message}";
                return result;
            }

            return result;
        }

        /// <summary>
        /// 특정 레시피의 Bill 찾기
        /// </summary>
        public static Bill? FindBill(Building_WorkTable workTable, RecipeDef recipe)
        {
            if (workTable == null || workTable.billStack == null || recipe == null)
                return null;

            return workTable.billStack.Bills.FirstOrDefault(b => b.recipe == recipe);
        }

        /// <summary>
        /// 작업대에 Bill이 너무 많은지 확인 (성능 보호)
        /// </summary>
        public static bool HasTooManyBills(Building_WorkTable workTable, int maxBills = 10)
        {
            if (workTable == null || workTable.billStack == null)
                return false;

            return workTable.billStack.Count >= maxBills;
        }

        /// <summary>
        /// Bill 우선순위 조정 (맨 위로 이동)
        /// </summary>
        public static BillResult MoveBillToTop(Building_WorkTable workTable, Bill bill)
        {
            var result = new BillResult();

            if (workTable == null || workTable.billStack == null)
            {
                result.ErrorMessage = "작업대가 유효하지 않습니다";
                return result;
            }

            if (bill == null)
            {
                result.ErrorMessage = "Bill이 null입니다";
                return result;
            }

            try
            {
                // 현재 인덱스 찾기
                int currentIndex = workTable.billStack.Bills.IndexOf(bill);
                if (currentIndex < 0)
                {
                    result.ErrorMessage = "Bill을 찾을 수 없습니다";
                    return result;
                }

                // 맨 위로 이동
                for (int i = 0; i < currentIndex; i++)
                {
                    workTable.billStack.Reorder(bill, -1);
                }

                result.Success = true;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"Bill 우선순위 조정 중 오류: {ex.Message}";
                return result;
            }

            return result;
        }

        /// <summary>
        /// 자원 가용성 확인 (Bill 생성 전 체크)
        /// </summary>
        public static bool HasSufficientResources(Map map, RecipeDef recipe, int count = 1, float bufferMultiplier = 1.2f)
        {
            if (recipe == null || recipe.ingredients == null || recipe.ingredients.Count == 0)
                return true; // 재료 불필요

            foreach (var ingredient in recipe.ingredients)
            {
                int required = (int)(ingredient.GetBaseCount() * count * bufferMultiplier);

                // 필터에 허용된 재료 중 하나라도 충분하면 OK
                bool hasEnough = false;
                foreach (var allowedDef in ingredient.filter.AllowedThingDefs)
                {
                    int available = map.resourceCounter.GetCount(allowedDef);
                    if (available >= required)
                    {
                        hasEnough = true;
                        break;
                    }
                }

                if (!hasEnough)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 재질 선택 (강철, 천, 가죽 중 가장 많은 것)
        /// </summary>
        public static ThingDef? SelectBestStuff(Map map, RecipeDef recipe)
        {
            if (recipe == null || recipe.products == null || recipe.products.Count == 0)
                return null;

            var product = recipe.products[0].thingDef;
            if (!product.MadeFromStuff)
                return null;

            // 허용된 재질 카테고리 가져오기
            var stuffCategories = product.stuffCategories;
            if (stuffCategories == null || stuffCategories.Count == 0)
                return null;

            // 가능한 재질 목록
            var candidates = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.IsStuff && stuffCategories.Any(cat => d.stuffProps.categories.Contains(cat)))
                .ToList();

            if (candidates.Count == 0)
                return null;

            // 가장 많은 재질 선택
            ThingDef? bestStuff = null;
            int maxCount = 0;

            foreach (var candidate in candidates)
            {
                int available = map.resourceCounter.GetCount(candidate);
                if (available > maxCount)
                {
                    maxCount = available;
                    bestStuff = candidate;
                }
            }

            return maxCount >= 10 ? bestStuff : null; // 최소 10개 이상 있어야 선택
        }
    }
}
